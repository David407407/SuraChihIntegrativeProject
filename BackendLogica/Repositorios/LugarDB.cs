using System.Data.Common;
using BackendLogica.Configuracion;
using BackendLogica.Datos;
using BackendLogica.Modelos;
using BackendLogica.Seguridad;

namespace BackendLogica.Repositorios
{
    /// <summary>
    /// Lugares (cafés, museos, parques...). Solo organizadores aprobados los registran y pasan por
    /// moderación; cualquier edición de un lugar aprobado lo regresa a revisión (trigger).
    /// </summary>
    public sealed class LugarDB : ConexionDB
    {
        private const string SelectTarjeta = """
            SELECT p.*, r.avg_rating, r.review_count
            FROM place p
            LEFT JOIN v_place_rating r ON r.place_id = p.id
            """;

        public LugarDB(ConfiguracionBD? configuracion = null) : base(configuracion) { }

        // ------------------------------------------------------------------
        // Consultas públicas
        // ------------------------------------------------------------------

        /// <summary>"Lugares para descubrir": aprobados, mejor calificados primero.</summary>
        public Task<List<LugarTarjeta>> ListarRecomendadosAsync(int limite = 4, CancellationToken ct = default) =>
            ConsultarTarjetasAsync($"""
                {SelectTarjeta}
                WHERE p.status = 'approved'
                ORDER BY r.avg_rating IS NULL, r.avg_rating DESC, r.review_count DESC, p.name
                LIMIT @limite
                """, new { limite }, ct);

        /// <summary>Todos los aprobados (pines del mapa). Opcionalmente solo los de una etiqueta.</summary>
        public Task<List<LugarTarjeta>> ListarAprobadosAsync(int? etiquetaId = null, CancellationToken ct = default) =>
            ConsultarTarjetasAsync($"""
                {SelectTarjeta}
                WHERE p.status = 'approved'
                  AND (@etiquetaId IS NULL OR EXISTS (SELECT 1 FROM place_tag pt
                                                       WHERE pt.place_id = p.id AND pt.tag_id = @etiquetaId))
                ORDER BY p.name
                """, new { etiquetaId }, ct);

        /// <summary>Búsqueda por nombre, dirección o etiqueta.</summary>
        public Task<List<LugarTarjeta>> BuscarAsync(string texto, int limite = 30, CancellationToken ct = default) =>
            ConsultarTarjetasAsync($"""
                {SelectTarjeta}
                WHERE p.status = 'approved'
                  AND (p.name LIKE CONCAT('%', @texto, '%')
                       OR p.address LIKE CONCAT('%', @texto, '%')
                       OR EXISTS (SELECT 1 FROM place_tag pt JOIN tag t ON t.id = pt.tag_id
                                  WHERE pt.place_id = p.id AND t.name LIKE CONCAT('%', @texto, '%')))
                ORDER BY p.name
                LIMIT @limite
                """, new { texto = texto.Trim(), limite }, ct);

        public async Task<int> ContarAprobadosAsync(CancellationToken ct = default) =>
            await EscalarAsync<int>("SELECT COUNT(*) FROM place WHERE status = 'approved'", ct: ct);

        /// <summary>Detalle con galería, en cualquier estado.</summary>
        public async Task<LugarDetalle?> ObtenerAsync(int lugarId, CancellationToken ct = default)
        {
            var lugar = (await ConsultarTarjetasAsync($"{SelectTarjeta} WHERE p.id = @lugarId", new { lugarId }, ct)).FirstOrDefault();
            if (lugar == null) return null;

            var galeria = await ConsultarAsync(
                "SELECT url FROM place_image WHERE place_id = @lugarId ORDER BY sort_order, id",
                r => r.Texto("url"), new { lugarId }, ct);
            return new LugarDetalle(lugar, galeria);
        }

        // ------------------------------------------------------------------
        // Dueño / moderación
        // ------------------------------------------------------------------

        public Task<List<LugarTarjeta>> ListarDeDuenoAsync(int duenoId, CancellationToken ct = default) =>
            ConsultarTarjetasAsync($"{SelectTarjeta} WHERE p.owner_id = @duenoId ORDER BY p.name", new { duenoId }, ct);

        public Task<List<LugarTarjeta>> ListarPorEstadoAsync(EstadoLugar estado, CancellationToken ct = default) =>
            ConsultarTarjetasAsync($"{SelectTarjeta} WHERE p.status = @estado ORDER BY p.created_at", new { estado }, ct);

        /// <summary>Registra un lugar con etiquetas y horario. Queda pendiente de moderación.</summary>
        /// <returns>Id del lugar nuevo.</returns>
        public Task<int> CrearAsync(int duenoId, DatosLugar datos, CancellationToken ct = default)
        {
            ValidarDatos(datos);
            return EnTransaccionAsync(async tx =>
            {
                int id = await tx.InsertarAsync("""
                    INSERT INTO place (owner_id, name, description, address, latitude, longitude, phone,
                                       website_url, price_min, price_max, main_image_url)
                    VALUES (@duenoId, @Nombre, @Descripcion, @Direccion, @Latitud, @Longitud, @Telefono,
                            @SitioWebUrl, @PrecioMin, @PrecioMax, @ImagenUrl)
                    """, ParametrosLugar(duenoId, datos));
                await GuardarEtiquetasYHorarioAsync(tx, id, datos);
                return id;
            }, ct);
        }

        /// <returns>false si el lugar no existe o no es de este dueño.</returns>
        public Task<bool> ActualizarAsync(int lugarId, int duenoId, DatosLugar datos, CancellationToken ct = default)
        {
            ValidarDatos(datos);
            return EnTransaccionAsync(async tx =>
            {
                int filas = await tx.EjecutarAsync("""
                    UPDATE place SET name = @Nombre, description = @Descripcion, address = @Direccion,
                           latitude = @Latitud, longitude = @Longitud, phone = @Telefono,
                           website_url = @SitioWebUrl, price_min = @PrecioMin, price_max = @PrecioMax,
                           main_image_url = @ImagenUrl
                    WHERE id = @lugarId AND owner_id = @duenoId
                    """, ParametrosLugar(duenoId, datos, lugarId));
                if (filas == 0) return false;

                await tx.EjecutarAsync("DELETE FROM place_tag WHERE place_id = @lugarId", new { lugarId });
                await tx.EjecutarAsync("DELETE FROM place_hours WHERE place_id = @lugarId", new { lugarId });
                await GuardarEtiquetasYHorarioAsync(tx, lugarId, datos);
                return true;
            }, ct);
        }

        /// <summary>Agrega una foto a la galería. Si el lugar estaba aprobado, vuelve a moderación.</summary>
        public Task<int> AgregarImagenAsync(int lugarId, string url, CancellationToken ct = default) =>
            InsertarAsync("""
                INSERT INTO place_image (place_id, url, sort_order)
                SELECT @lugarId, @url, COALESCE(MAX(sort_order), 0) + 1 FROM place_image WHERE place_id = @lugarId
                """, new { lugarId, url }, ct);

        // ------------------------------------------------------------------
        // Apoyo
        // ------------------------------------------------------------------

        /// <summary>Ejecuta el SELECT y completa etiquetas, horario y estado "abierto ahora".</summary>
        private async Task<List<LugarTarjeta>> ConsultarTarjetasAsync(string sql, object? parametros, CancellationToken ct)
        {
            var lugares = await ConsultarAsync(sql, Mapear, parametros, ct);
            if (lugares.Count == 0) return lugares;
            var ids = new { ids = lugares.Select(l => l.Id) };

            var etiquetas = (await ConsultarAsync("""
                SELECT pt.place_id, t.*
                FROM place_tag pt JOIN tag t ON t.id = pt.tag_id
                WHERE pt.place_id IN @ids AND t.is_active
                ORDER BY t.sort_order, t.name
                """, r => (LugarId: r.Entero("place_id"), Etiqueta: EtiquetaDB.Mapear(r)), ids, ct))
                .ToLookup(f => f.LugarId, f => f.Etiqueta);

            var horarios = (await ConsultarAsync(
                "SELECT * FROM place_hours WHERE place_id IN @ids ORDER BY day_of_week",
                r => (LugarId: r.Entero("place_id"),
                      Horario: new HorarioDia((DayOfWeek)r.Entero("day_of_week"), r.Hora("opens_at"), r.Hora("closes_at"))),
                ids, ct))
                .ToLookup(f => f.LugarId, f => f.Horario);

            DateTime ahora = DateTime.Now;
            return lugares.Select(l => l with
            {
                Etiquetas = etiquetas[l.Id].ToList(),
                Horario = horarios[l.Id].ToList(),
                HorarioAhora = EstadoHorario.Calcular(horarios[l.Id], ahora)
            }).ToList();
        }

        private static LugarTarjeta Mapear(DbDataReader r) => new()
        {
            Id = r.Entero("id"),
            Nombre = r.Texto("name"),
            Descripcion = r.TextoONulo("description"),
            Direccion = r.Texto("address"),
            Latitud = r.Decimal("latitude"),
            Longitud = r.Decimal("longitude"),
            Telefono = r.TextoONulo("phone"),
            SitioWebUrl = r.TextoONulo("website_url"),
            PrecioMin = r.DecimalONulo("price_min"),
            PrecioMax = r.DecimalONulo("price_max"),
            ImagenUrl = r.TextoONulo("main_image_url"),
            Estado = r.Enum<EstadoLugar>("status"),
            DuenoId = r.Entero("owner_id"),
            Calificacion = r.DecimalONulo("avg_rating"),
            NumResenas = r.EsNulo("review_count") ? 0 : r.Entero("review_count")
        };

        private static object ParametrosLugar(int duenoId, DatosLugar d, int lugarId = 0) => new
        {
            duenoId, lugarId,
            Nombre = d.Nombre.Trim(), d.Descripcion, Direccion = d.Direccion.Trim(), d.Latitud, d.Longitud,
            d.Telefono, d.SitioWebUrl, d.PrecioMin, d.PrecioMax, d.ImagenUrl
        };

        private static async Task GuardarEtiquetasYHorarioAsync(Transaccion tx, int lugarId, DatosLugar d)
        {
            foreach (int etiquetaId in d.EtiquetaIds.Distinct())
                await tx.EjecutarAsync("INSERT INTO place_tag (place_id, tag_id) VALUES (@lugarId, @etiquetaId)", new { lugarId, etiquetaId });

            foreach (var h in d.Horario)
                await tx.EjecutarAsync("""
                    INSERT INTO place_hours (place_id, day_of_week, opens_at, closes_at)
                    VALUES (@lugarId, @dia, @abre, @cierra)
                    """, new { lugarId, dia = (int)h.Dia, abre = h.Abre, cierra = h.Cierra });
        }

        private static void ValidarDatos(DatosLugar d)
        {
            Validar.Requerido(d.Nombre, "El nombre");
            Validar.Requerido(d.Direccion, "La dirección");
            Validar.CoordenadasChihuahua(d.Latitud, d.Longitud);
            Validar.RangoPrecio(d.PrecioMin, d.PrecioMax);
            if (d.Horario.GroupBy(h => h.Dia).Any(g => g.Count() > 1))
                throw new DatosInvalidosException("Cada día solo puede tener un horario.");
            if (d.Horario.Any(h => h.Abre == h.Cierra))
                throw new DatosInvalidosException("La hora de apertura y de cierre no pueden ser iguales.");
        }
    }
}
