using BackendLogica.Configuracion;
using BackendLogica.Datos;
using BackendLogica.Modelos;
using BackendLogica.Seguridad;

namespace BackendLogica.Repositorios
{
    /// <summary>
    /// Eventos: listados públicos (inicio, búsqueda, filtros), detalle y gestión del organizador.
    /// Las reglas de publicación (solo organizadores aprobados, volver a moderación al editar...)
    /// viven en los triggers de la BD y llegan como <see cref="ReglaNegocioException"/>.
    /// </summary>
    public sealed class EventoDB : ConsultasEventoDB
    {
        public EventoDB(ConfiguracionBD? configuracion = null) : base(configuracion) { }

        // ------------------------------------------------------------------
        // Consultas públicas
        // ------------------------------------------------------------------

        /// <summary>Eventos que los moderadores marcaron como destacados (carrusel del hero).</summary>
        public Task<List<EventoTarjeta>> ListarDestacadosAsync(int limite = 5, CancellationToken ct = default) =>
            ConsultarTarjetasAsync($"""
                {SelectTarjeta}
                WHERE {Vigente} AND c.is_featured
                ORDER BY c.featured_order IS NULL, c.featured_order, c.start_at
                LIMIT @limite
                """, new { limite }, ct);

        /// <summary>"No te los pierdas": los vigentes con más corazones primero.</summary>
        public Task<List<EventoTarjeta>> ListarMasGuardadosAsync(int limite = 5, CancellationToken ct = default) =>
            ConsultarTarjetasAsync($"""
                {SelectTarjeta}
                WHERE {Vigente}
                ORDER BY interested DESC, c.start_at
                LIMIT @limite
                """, new { limite }, ct);

        /// <summary>
        /// Inicio del usuario: próximos eventos, con sus favoritos primero.
        /// Sin <paramref name="usuarioId"/> (visitante) se ordenan solo por fecha.
        /// </summary>
        public Task<List<EventoTarjeta>> ListarProximosAsync(int? usuarioId = null, int limite = 30, CancellationToken ct = default) =>
            ConsultarTarjetasAsync($"""
                {SelectTarjeta}
                LEFT JOIN favorite mine ON mine.event_id = c.id AND mine.user_id = @usuarioId
                WHERE {Vigente}
                ORDER BY (mine.user_id IS NOT NULL) DESC, c.start_at
                LIMIT @limite
                """, new { usuarioId, limite }, ct);

        /// <summary>Búsqueda por título, lugar o nombre de etiqueta (sin distinguir acentos ni mayúsculas).</summary>
        public Task<List<EventoTarjeta>> BuscarAsync(string texto, int limite = 30, CancellationToken ct = default) =>
            ConsultarTarjetasAsync($"""
                {SelectTarjeta}
                WHERE {Vigente}
                  AND (c.title LIKE CONCAT('%', @texto, '%')
                       OR c.place_name LIKE CONCAT('%', @texto, '%')
                       OR EXISTS (SELECT 1 FROM event_tag et JOIN tag t ON t.id = et.tag_id
                                  WHERE et.event_id = c.id AND t.name LIKE CONCAT('%', @texto, '%')))
                ORDER BY c.start_at
                LIMIT @limite
                """, new { texto = texto.Trim(), limite }, ct);

        /// <summary>Filtro por chips: eventos que tienen TODAS las etiquetas indicadas.</summary>
        public Task<List<EventoTarjeta>> FiltrarPorEtiquetasAsync(IReadOnlyCollection<int> etiquetaIds, int limite = 30, CancellationToken ct = default)
        {
            if (etiquetaIds.Count == 0) return ListarProximosAsync(null, limite, ct);
            return ConsultarTarjetasAsync($"""
                {SelectTarjeta}
                WHERE {Vigente}
                  AND (SELECT COUNT(DISTINCT et.tag_id) FROM event_tag et
                       WHERE et.event_id = c.id AND et.tag_id IN @etiquetas) = @total
                ORDER BY c.start_at
                LIMIT @limite
                """, new { etiquetas = etiquetaIds, total = etiquetaIds.Distinct().Count(), limite }, ct);
        }

        /// <summary>Eventos vigentes que ocurren entre hoy y el domingo (inclusive).</summary>
        public async Task<int> ContarEstaSemanaAsync(CancellationToken ct = default) =>
            await EscalarAsync<int>($"""
                SELECT COUNT(*) FROM v_event_card c
                WHERE {Vigente} AND c.start_at < @finSemana
                """, new { finSemana = FinDeSemana(DateTime.Now) }, ct);

        /// <summary>Detalle con galería. Devuelve el evento en cualquier estado (el llamador decide si mostrarlo).</summary>
        public async Task<EventoDetalle?> ObtenerAsync(int eventoId, CancellationToken ct = default)
        {
            var evento = (await ConsultarTarjetasAsync($"{SelectTarjeta} WHERE c.id = @eventoId", new { eventoId }, ct)).FirstOrDefault();
            if (evento == null) return null;

            var galeria = await ConsultarAsync(
                "SELECT url FROM event_image WHERE event_id = @eventoId ORDER BY sort_order, id",
                r => r.Texto("url"), new { eventoId }, ct);
            return new EventoDetalle(evento, galeria);
        }

        /// <summary>
        /// Registra una vista para el embudo del organizador. Máximo una por usuario por evento por día;
        /// las de visitantes (<paramref name="usuarioId"/> null) siempre cuentan.
        /// </summary>
        public Task<int> RegistrarVistaAsync(int eventoId, int? usuarioId, CancellationToken ct = default) =>
            EjecutarAsync("""
                INSERT INTO event_view (event_id, user_id)
                SELECT @eventoId, @usuarioId FROM DUAL
                WHERE @usuarioId IS NULL OR NOT EXISTS (
                    SELECT 1 FROM event_view
                    WHERE event_id = @eventoId AND user_id = @usuarioId AND viewed_at >= CURDATE())
                """, new { eventoId, usuarioId }, ct);

        // ------------------------------------------------------------------
        // Organizador
        // ------------------------------------------------------------------

        /// <summary>Todos los eventos del organizador, del más reciente al más antiguo.</summary>
        public Task<List<EventoTarjeta>> ListarDeOrganizadorAsync(int organizadorId, CancellationToken ct = default) =>
            ConsultarTarjetasAsync($"""
                {SelectTarjeta}
                WHERE c.organizer_id = @organizadorId
                ORDER BY c.start_at DESC
                """, new { organizadorId }, ct);

        /// <summary>Eventos en un estado dado (ej. Pendiente para el panel de moderación).</summary>
        public Task<List<EventoTarjeta>> ListarPorEstadoAsync(EstadoEvento estado, CancellationToken ct = default) =>
            ConsultarTarjetasAsync($"""
                {SelectTarjeta}
                WHERE c.status = @estado
                ORDER BY c.start_at
                """, new { estado }, ct);

        /// <summary>Embudo vistas → interesados → inscritos y calificación de cada evento del organizador.</summary>
        public Task<List<EstadisticasEvento>> ListarEstadisticasAsync(int organizadorId, CancellationToken ct = default) =>
            ConsultarAsync("""
                SELECT * FROM v_event_stats WHERE organizer_id = @organizadorId
                """, r => new EstadisticasEvento(
                    r.Entero("event_id"), r.Entero("views"), r.Entero("interested"),
                    r.Entero("inscribed"), r.DecimalONulo("avg_rating"), r.Entero("review_count")),
                new { organizadorId }, ct);

        /// <summary>
        /// Crea un evento con sus etiquetas. Con <paramref name="enviarARevision"/> = false queda como borrador.
        /// </summary>
        /// <returns>Id del evento nuevo.</returns>
        public Task<int> CrearAsync(int organizadorId, DatosEvento datos, bool enviarARevision = true, CancellationToken ct = default)
        {
            ValidarDatos(datos);
            return EnTransaccionAsync(async tx =>
            {
                int id = await tx.InsertarAsync("""
                    INSERT INTO event (organizer_id, place_id, title, description, start_at, end_at,
                                       address, latitude, longitude, price_min, price_max,
                                       ticket_url, main_image_url, status)
                    VALUES (@organizadorId, @LugarId, @Titulo, @Descripcion, @Inicio, @Fin,
                            @Direccion, @Latitud, @Longitud, @PrecioMin, @PrecioMax,
                            @BoletosUrl, @ImagenUrl, @estado)
                    """, ParametrosEvento(organizadorId, datos, enviarARevision ? EstadoEvento.Pendiente : EstadoEvento.Borrador));
                await GuardarEtiquetasAsync(tx, id, datos.EtiquetaIds);
                return id;
            }, ct);
        }

        /// <summary>
        /// Edita un evento propio. Si ya estaba aprobado, la BD lo regresa a moderación automáticamente.
        /// </summary>
        /// <returns>false si el evento no existe o no es de este organizador.</returns>
        public Task<bool> ActualizarAsync(int eventoId, int organizadorId, DatosEvento datos, CancellationToken ct = default)
        {
            ValidarDatos(datos);
            return EnTransaccionAsync(async tx =>
            {
                int filas = await tx.EjecutarAsync("""
                    UPDATE event SET place_id = @LugarId, title = @Titulo, description = @Descripcion,
                           start_at = @Inicio, end_at = @Fin, address = @Direccion,
                           latitude = @Latitud, longitude = @Longitud, price_min = @PrecioMin,
                           price_max = @PrecioMax, ticket_url = @BoletosUrl, main_image_url = @ImagenUrl
                    WHERE id = @eventoId AND organizer_id = @organizadorId AND status <> 'cancelled'
                    """, ParametrosEvento(organizadorId, datos, null, eventoId));
                if (filas == 0) return false;

                await tx.EjecutarAsync("DELETE FROM event_tag WHERE event_id = @eventoId", new { eventoId });
                await GuardarEtiquetasAsync(tx, eventoId, datos.EtiquetaIds);
                return true;
            }, ct);
        }

        /// <summary>Manda a moderación un borrador o un evento rechazado ya corregido.</summary>
        public async Task<bool> EnviarARevisionAsync(int eventoId, int organizadorId, CancellationToken ct = default) =>
            await EjecutarAsync("""
                UPDATE event SET status = 'pending'
                WHERE id = @eventoId AND organizer_id = @organizadorId AND status IN ('draft', 'rejected')
                """, new { eventoId, organizadorId }, ct) > 0;

        /// <summary>Cancela un evento propio. El motivo se muestra a los inscritos.</summary>
        public async Task<bool> CancelarAsync(int eventoId, int organizadorId, string motivo, CancellationToken ct = default)
        {
            Validar.Requerido(motivo, "El motivo de cancelación");
            return await EjecutarAsync("""
                UPDATE event SET status = 'cancelled', cancel_reason = @motivo, is_featured = FALSE
                WHERE id = @eventoId AND organizer_id = @organizadorId AND status <> 'cancelled'
                """, new { eventoId, organizadorId, motivo = motivo.Trim() }, ct) > 0;
        }

        /// <summary>Agrega una foto a la galería (al final). Si el evento estaba aprobado, vuelve a moderación.</summary>
        public Task<int> AgregarImagenAsync(int eventoId, string url, CancellationToken ct = default) =>
            InsertarAsync("""
                INSERT INTO event_image (event_id, url, sort_order)
                SELECT @eventoId, @url, COALESCE(MAX(sort_order), 0) + 1 FROM event_image WHERE event_id = @eventoId
                """, new { eventoId, url }, ct);

        // ------------------------------------------------------------------
        // Apoyo
        // ------------------------------------------------------------------

        private static object ParametrosEvento(int organizadorId, DatosEvento d, EstadoEvento? estado, int eventoId = 0) => new
        {
            organizadorId, eventoId, estado,
            d.LugarId, Titulo = d.Titulo.Trim(), Descripcion = d.Descripcion.Trim(), d.Inicio, d.Fin,
            d.Direccion, d.Latitud, d.Longitud, d.PrecioMin, d.PrecioMax, d.BoletosUrl, d.ImagenUrl
        };

        private static async Task GuardarEtiquetasAsync(Transaccion tx, int eventoId, IReadOnlyCollection<int> etiquetaIds)
        {
            foreach (int etiquetaId in etiquetaIds.Distinct())
                await tx.EjecutarAsync("INSERT INTO event_tag (event_id, tag_id) VALUES (@eventoId, @etiquetaId)", new { eventoId, etiquetaId });
        }

        private static void ValidarDatos(DatosEvento d)
        {
            Validar.Requerido(d.Titulo, "El título");
            Validar.Requerido(d.Descripcion, "La descripción");
            if (d.Fin <= d.Inicio) throw new DatosInvalidosException("La fecha de fin debe ser posterior al inicio.");
            Validar.RangoPrecio(d.PrecioMin, d.PrecioMax);
            if (d.LugarId == null)
            {
                Validar.Requerido(d.Direccion, "La dirección");
                if (d.Latitud == null || d.Longitud == null)
                    throw new DatosInvalidosException("Marca la ubicación del evento en el mapa.");
            }
            if (d.Latitud.HasValue && d.Longitud.HasValue)
                Validar.CoordenadasChihuahua(d.Latitud.Value, d.Longitud.Value);
        }

        /// <summary>Lunes siguiente a las 00:00 (la semana termina el domingo).</summary>
        private static DateTime FinDeSemana(DateTime hoy)
        {
            int diasHastaLunes = ((int)DayOfWeek.Monday - (int)hoy.DayOfWeek + 7) % 7;
            return hoy.Date.AddDays(diasHastaLunes == 0 ? 7 : diasHastaLunes);
        }
    }
}
