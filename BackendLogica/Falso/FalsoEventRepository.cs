using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;
using BackendLogica.Repositorios;
using BackendLogica.Seguridad;

namespace BackendLogica.Falso
{
    /// <summary>
    /// <see cref="IEventRepository"/> en memoria. Repite las consultas de <c>EventoDB</c> y las reglas de los
    /// triggers <c>trg_event_bi</c>, <c>trg_event_bu</c> y <c>trg_eventimg_ai</c>. A diferencia de MySQL v1,
    /// acepta borradores sin ubicación y fecha o precio por confirmar.
    /// </summary>
    public sealed class FalsoEventRepository : RepositorioFalso, IEventRepository
    {
        internal FalsoEventRepository(AlmacenFalso almacen) : base(almacen) { }

        // ------------------------------------------------------------------
        // Consultas públicas
        // ------------------------------------------------------------------

        public Task<List<EventoTarjeta>> ListarDestacadosAsync(int limite = 5, CancellationToken ct = default) =>
            Hacer(a => Tarjetas(a, a.Eventos
                .Where(e => AlmacenFalso.Vigente(e, Ahora) && e.Destacado)
                .OrderBy(e => e.OrdenDestacado == null).ThenBy(e => e.OrdenDestacado).ThenBy(AlmacenFalso.OrdenFecha)
                .Take(limite)), ct);

        public Task<List<EventoTarjeta>> ListarMasGuardadosAsync(int limite = 5, CancellationToken ct = default) =>
            Hacer(a => Tarjetas(a, a.Eventos
                .Where(e => AlmacenFalso.Vigente(e, Ahora))
                .OrderByDescending(e => a.Favoritos.Count(f => f.EventoId == e.Id)).ThenBy(AlmacenFalso.OrdenFecha)
                .Take(limite)), ct);

        public Task<List<EventoTarjeta>> ListarProximosAsync(int? usuarioId = null, int limite = 30, CancellationToken ct = default) =>
            Hacer(a => Proximos(a, usuarioId, limite), ct);

        public Task<List<EventoTarjeta>> BuscarAsync(string texto, int limite = 30, CancellationToken ct = default) =>
            Hacer(a => Tarjetas(a, a.Eventos
                .Where(e => AlmacenFalso.Vigente(e, Ahora) && Coincide(a, e, texto.Trim()))
                .OrderBy(AlmacenFalso.OrdenFecha)
                .Take(limite)), ct);

        public Task<List<EventoTarjeta>> FiltrarPorEtiquetasAsync(IReadOnlyCollection<int> etiquetaIds, int limite = 30, CancellationToken ct = default) =>
            Hacer(a => etiquetaIds.Count == 0
                ? Proximos(a, null, limite)
                : Tarjetas(a, a.Eventos
                    .Where(e => AlmacenFalso.Vigente(e, Ahora) && etiquetaIds.All(e.EtiquetaIds.Contains))
                    .OrderBy(AlmacenFalso.OrdenFecha)
                    .Take(limite)), ct);

        public Task<List<EventoTarjeta>> FiltrarAsync(FiltroEventos filtro, CancellationToken ct = default) =>
            Hacer(a =>
            {
                var rango = filtro.RangoFechas(Ahora);
                string? texto = string.IsNullOrWhiteSpace(filtro.Texto) ? null : filtro.Texto.Trim();
                return Tarjetas(a, a.Eventos
                    .Where(e => AlmacenFalso.Vigente(e, Ahora))
                    .Where(e => texto == null || Coincide(a, e, texto))
                    .Where(e => filtro.EtiquetaIds.Count == 0 || filtro.EtiquetaIds.Any(e.EtiquetaIds.Contains))
                    .Where(e => filtro.Precio switch
                    {
                        FiltroPrecio.Gratis => !e.PrecioPorConfirmar && e.PrecioMax == 0,
                        FiltroPrecio.DePago => !e.PrecioPorConfirmar && e.PrecioMax > 0,
                        _ => true
                    })
                    // Con fecha por confirmar no se puede saber si cae en el rango: se excluye.
                    .Where(e => rango == null || (!e.FechaPorConfirmar && e.Inicio < rango.Value.Hasta && e.Fin > rango.Value.Desde))
                    .OrderBy(AlmacenFalso.OrdenFecha)
                    .Take(filtro.Limite));
            }, ct);

        public Task<int> ContarEstaSemanaAsync(CancellationToken ct = default) =>
            Hacer(a =>
            {
                DateTime lunes = FinDeSemana(Ahora);
                return a.Eventos.Count(e => AlmacenFalso.Vigente(e, Ahora) && !e.FechaPorConfirmar && e.Inicio < lunes);
            }, ct);

        public Task<EventoDetalle?> ObtenerAsync(int eventoId, CancellationToken ct = default) =>
            Hacer(a =>
            {
                var e = a.Evento(eventoId);
                return e == null ? null : new EventoDetalle(a.Tarjeta(e, Ahora),
                    e.Galeria.OrderBy(i => i.Orden).ThenBy(i => i.Id).Select(i => i.Url).ToList());
            }, ct);

        public Task<int> RegistrarVistaAsync(int eventoId, int? usuarioId, CancellationToken ct = default) =>
            Hacer(a =>
            {
                if (a.Evento(eventoId) == null) throw AlmacenFalso.NoExiste();
                bool yaVioHoy = usuarioId != null && a.Vistas.Any(v =>
                    v.EventoId == eventoId && v.UsuarioId == usuarioId && v.VistoEn >= DateTime.Today);
                if (yaVioHoy) return 0;
                a.Vistas.Add(new FilaVista(a.SiguienteVista(), eventoId, usuarioId, Ahora));
                return 1;
            }, ct);

        // ------------------------------------------------------------------
        // Organizador
        // ------------------------------------------------------------------

        public Task<List<EventoTarjeta>> ListarDeOrganizadorAsync(int organizadorId, CancellationToken ct = default) =>
            Hacer(a => Tarjetas(a, a.Eventos.Where(e => e.OrganizadorId == organizadorId)
                .OrderByDescending(e => e.FechaPorConfirmar).ThenByDescending(e => e.Inicio)), ct);

        public Task<List<EventoTarjeta>> ListarPorEstadoAsync(EstadoEvento estado, CancellationToken ct = default) =>
            Hacer(a => Tarjetas(a, a.Eventos.Where(e => e.Estado == estado).OrderBy(AlmacenFalso.OrdenFecha)), ct);

        /// <summary>Misma cuenta que la vista <c>v_event_stats</c>.</summary>
        public Task<List<EstadisticasEvento>> ListarEstadisticasAsync(int organizadorId, CancellationToken ct = default) =>
            Hacer(a => a.Eventos.Where(e => e.OrganizadorId == organizadorId).Select(e =>
            {
                var resenas = Repositorios.ResenaDB.Resumir(a.Resenas
                    .Where(r => r.Tipo == TipoResena.Evento && r.ObjetivoId == e.Id && !r.Oculta)
                    .GroupBy(r => r.Calificacion).Select(g => (g.Key, g.Count())));
                return new EstadisticasEvento(
                    e.Id,
                    a.Vistas.Count(v => v.EventoId == e.Id),
                    a.Favoritos.Count(f => f.EventoId == e.Id),
                    a.Inscripciones.Count(i => i.EventoId == e.Id && i.Estado == EstadoInscripcion.Activa),
                    resenas.Promedio,
                    resenas.Total);
            }).ToList(), ct);

        public Task<List<VistasDia>> ListarVistasPorDiaAsync(int eventoId, int dias = 7, CancellationToken ct = default) =>
            Hacer(a =>
            {
                DateTime desde = DateTime.Today.AddDays(1 - dias);
                var filas = a.Vistas.Where(v => v.EventoId == eventoId && v.VistoEn >= desde)
                                    .GroupBy(v => DateOnly.FromDateTime(v.VistoEn))
                                    .Select(g => (g.Key, g.Count()));
                return EventoDB.CompletarDias(filas, dias, DateTime.Today);
            }, ct);

        public Task<int> CrearAsync(int organizadorId, DatosEvento datos, bool enviarARevision = true, CancellationToken ct = default) =>
            Hacer(a =>
            {
                EventoDB.ValidarDatos(datos, esBorrador: !enviarARevision);

                // trg_event_bi
                if (!a.EsOrganizadorAprobado(organizadorId)) throw Trigger(MensajesTrigger.SoloOrganizadoresEventos);
                RevisarLugar(a, datos.LugarId);
                RevisarEtiquetas(a, datos.EtiquetaIds);

                var e = new FilaEvento
                {
                    Id = AlmacenFalso.Siguiente(a.Eventos, x => x.Id),
                    OrganizadorId = organizadorId,
                    Estado = enviarARevision ? EstadoEvento.Pendiente : EstadoEvento.Borrador,
                    CreadoEn = Ahora
                };
                Copiar(datos, e);
                a.Eventos.Add(e);
                return e.Id;
            }, ct);

        public Task<bool> ActualizarAsync(int eventoId, int organizadorId, DatosEvento datos, CancellationToken ct = default) =>
            Hacer(a =>
            {
                var e = a.Eventos.FirstOrDefault(x => x.Id == eventoId && x.OrganizadorId == organizadorId && x.Estado != EstadoEvento.Cancelado);
                EventoDB.ValidarDatos(datos, esBorrador: e?.Estado == EstadoEvento.Borrador);
                if (e == null) return false;

                // trg_event_bu: cambiar otro lugar exige que esté aprobado.
                if (datos.LugarId != null && datos.LugarId != e.LugarId) RevisarLugar(a, datos.LugarId);
                RevisarEtiquetas(a, datos.EtiquetaIds);

                // trg_event_bu: editar el contenido de un evento aprobado lo regresa a moderación.
                bool cambioContenido =
                    e.Titulo != datos.Titulo.Trim() || e.Descripcion != datos.Descripcion.Trim() ||
                    e.ImagenUrl != datos.ImagenUrl || e.BoletosUrl != datos.BoletosUrl || e.LugarId != datos.LugarId ||
                    e.Direccion != datos.Direccion || e.Latitud != datos.Latitud || e.Longitud != datos.Longitud;
                if (e.Estado == EstadoEvento.Aprobado && cambioContenido)
                {
                    e.Estado = EstadoEvento.Pendiente;
                    e.Destacado = false;
                }
                Copiar(datos, e);
                return true;
            }, ct);

        public Task<bool> EnviarARevisionAsync(int eventoId, int organizadorId, CancellationToken ct = default) =>
            Hacer(a =>
            {
                var e = a.Eventos.FirstOrDefault(x => x.Id == eventoId && x.OrganizadorId == organizadorId
                    && x.Estado is EstadoEvento.Borrador or EstadoEvento.Rechazado);
                if (e == null) return false;
                // En MySQL v1 un borrador siempre está completo; aquí puede no estarlo.
                EventoDB.ValidarDatos(ADatos(e), esBorrador: false);
                e.Estado = EstadoEvento.Pendiente;
                return true;
            }, ct);

        public Task<bool> CancelarAsync(int eventoId, int organizadorId, string motivo, CancellationToken ct = default) =>
            Hacer(a =>
            {
                Validar.Requerido(motivo, "El motivo de cancelación");
                var e = a.Eventos.FirstOrDefault(x => x.Id == eventoId && x.OrganizadorId == organizadorId && x.Estado != EstadoEvento.Cancelado);
                if (e == null) return false;
                e.Estado = EstadoEvento.Cancelado;
                e.MotivoCancelacion = motivo.Trim();
                e.Destacado = false;
                return true;
            }, ct);

        public Task<int> AgregarImagenAsync(int eventoId, string url, CancellationToken ct = default) =>
            Hacer(a =>
            {
                var e = a.Evento(eventoId) ?? throw AlmacenFalso.NoExiste();
                var imagen = new FilaImagen(a.SiguienteImagenEvento(), url, e.Galeria.Select(i => i.Orden).DefaultIfEmpty(0).Max() + 1);
                e.Galeria.Add(imagen);
                // trg_eventimg_ai
                if (e.Estado == EstadoEvento.Aprobado)
                {
                    e.Estado = EstadoEvento.Pendiente;
                    e.Destacado = false;
                }
                return imagen.Id;
            }, ct);

        // ------------------------------------------------------------------
        // Apoyo
        // ------------------------------------------------------------------

        private static List<EventoTarjeta> Tarjetas(AlmacenFalso a, IEnumerable<FilaEvento> eventos) =>
            eventos.Select(e => a.Tarjeta(e, Ahora)).ToList();

        private static List<EventoTarjeta> Proximos(AlmacenFalso a, int? usuarioId, int limite) =>
            Tarjetas(a, a.Eventos
                .Where(e => AlmacenFalso.Vigente(e, Ahora))
                .OrderByDescending(e => usuarioId != null && a.Favoritos.Any(f => f.EventoId == e.Id && f.UsuarioId == usuarioId))
                .ThenBy(AlmacenFalso.OrdenFecha)
                .Take(limite));

        /// <summary>Título, nombre del lugar o nombre de alguna etiqueta.</summary>
        private static bool Coincide(AlmacenFalso a, FilaEvento e, string texto) =>
            AlmacenFalso.Contiene(e.Titulo, texto)
            || AlmacenFalso.Contiene(a.Lugar(e.LugarId)?.Nombre, texto)
            || a.EtiquetasDe(e.EtiquetaIds).Any(t => AlmacenFalso.Contiene(t.Nombre, texto));

        private static void RevisarLugar(AlmacenFalso a, int? lugarId)
        {
            if (lugarId != null && a.Lugar(lugarId)?.Estado != EstadoLugar.Aprobado)
                throw Trigger(MensajesTrigger.LugarNoAprobado);
        }

        private static void RevisarEtiquetas(AlmacenFalso a, IEnumerable<int> etiquetaIds)
        {
            if (etiquetaIds.Any(id => a.Etiquetas.All(t => t.Datos.Id != id))) throw AlmacenFalso.NoExiste();
        }

        private static void Copiar(DatosEvento d, FilaEvento e)
        {
            e.LugarId = d.LugarId;
            e.Titulo = d.Titulo.Trim();
            e.Descripcion = d.Descripcion.Trim();
            e.Inicio = d.Inicio;
            e.Fin = d.Fin;
            e.FechaPorConfirmar = d.FechaPorConfirmar;
            e.Direccion = d.Direccion;
            e.Latitud = d.Latitud;
            e.Longitud = d.Longitud;
            e.PrecioMin = d.PrecioMin;
            e.PrecioMax = d.PrecioMax;
            e.PrecioPorConfirmar = d.PrecioPorConfirmar;
            e.BoletosUrl = d.BoletosUrl;
            e.ImagenUrl = d.ImagenUrl;
            e.EtiquetaIds = d.EtiquetaIds.Distinct().ToList();
        }

        private static DatosEvento ADatos(FilaEvento e) => new()
        {
            LugarId = e.LugarId, Titulo = e.Titulo, Descripcion = e.Descripcion, Inicio = e.Inicio, Fin = e.Fin,
            FechaPorConfirmar = e.FechaPorConfirmar, PrecioPorConfirmar = e.PrecioPorConfirmar,
            Direccion = e.Direccion, Latitud = e.Latitud, Longitud = e.Longitud,
            PrecioMin = e.PrecioMin, PrecioMax = e.PrecioMax, BoletosUrl = e.BoletosUrl, ImagenUrl = e.ImagenUrl,
            EtiquetaIds = e.EtiquetaIds
        };

        /// <summary>Lunes siguiente a las 00:00 (igual que EventoDB).</summary>
        private static DateTime FinDeSemana(DateTime hoy)
        {
            int diasHastaLunes = ((int)DayOfWeek.Monday - (int)hoy.DayOfWeek + 7) % 7;
            return hoy.Date.AddDays(diasHastaLunes == 0 ? 7 : diasHastaLunes);
        }
    }
}
