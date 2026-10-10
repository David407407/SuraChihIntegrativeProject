using BackendLogica.Modelos;

namespace BackendLogica.Contratos
{
    /// <summary>
    /// Eventos: inicio, búsqueda, filtros, detalle y lo que hace el organizador (publicar, editar,
    /// panel y estadísticas). Las tarjetas salen de la vista <c>v_event_card</c> y las cifras de <c>v_event_stats</c>.
    /// Implementaciones: <c>EventoDB</c> y <c>FalsoEventRepository</c>.
    /// </summary>
    /// <remarks>
    /// Las reglas de publicación viven en los triggers y llegan como <see cref="Datos.ReglaNegocioException"/>
    /// con el texto del trigger, listo para mostrarse.
    /// </remarks>
    public interface IEventRepository
    {
        // --- Consultas públicas ------------------------------------------------

        /// <summary>Carrusel destacado del inicio (lo deciden los moderadores).</summary>
        Task<List<EventoTarjeta>> ListarDestacadosAsync(int limite = 5, CancellationToken ct = default);

        /// <summary>"Recomendados para ti" / "Lo más guardado": vigentes con más corazones primero.</summary>
        Task<List<EventoTarjeta>> ListarMasGuardadosAsync(int limite = 5, CancellationToken ct = default);

        /// <summary>Próximos eventos con los favoritos del usuario primero. Sin usuario, solo por fecha.</summary>
        Task<List<EventoTarjeta>> ListarProximosAsync(int? usuarioId = null, int limite = 30, CancellationToken ct = default);

        /// <summary>Buscador: título, lugar o nombre de etiqueta (sin distinguir acentos ni mayúsculas).</summary>
        Task<List<EventoTarjeta>> BuscarAsync(string texto, int limite = 30, CancellationToken ct = default);

        /// <summary>Eventos que tienen TODAS las etiquetas indicadas (filas por categoría del inicio).</summary>
        Task<List<EventoTarjeta>> FiltrarPorEtiquetasAsync(IReadOnlyCollection<int> etiquetaIds, int limite = 30, CancellationToken ct = default);

        /// <summary>
        /// NUEVO para el rediseño: modal "Filtros", chips Hoy / Este fin de semana / Gratis y modal Calendario.
        /// </summary>
        Task<List<EventoTarjeta>> FiltrarAsync(FiltroEventos filtro, CancellationToken ct = default);

        /// <summary>Eventos vigentes entre hoy y el domingo (hero de la landing).</summary>
        Task<int> ContarEstaSemanaAsync(CancellationToken ct = default);

        /// <summary>Detalle con galería, en cualquier estado (la pantalla decide si lo muestra).</summary>
        Task<EventoDetalle?> ObtenerAsync(int eventoId, CancellationToken ct = default);

        /// <summary>Registra una vista para el embudo. Máximo una por usuario por evento por día.</summary>
        Task<int> RegistrarVistaAsync(int eventoId, int? usuarioId, CancellationToken ct = default);

        // --- Organizador ---------------------------------------------------------

        /// <summary>Tabla "Mis eventos" del panel, del más reciente al más antiguo.</summary>
        Task<List<EventoTarjeta>> ListarDeOrganizadorAsync(int organizadorId, CancellationToken ct = default);

        /// <summary>Eventos en un estado (Pendiente para "Moderación / Publicaciones").</summary>
        Task<List<EventoTarjeta>> ListarPorEstadoAsync(EstadoEvento estado, CancellationToken ct = default);

        /// <summary>Embudo vistas → interesados → inscritos y calificación por evento (<c>v_event_stats</c>).</summary>
        Task<List<EstadisticasEvento>> ListarEstadisticasAsync(int organizadorId, CancellationToken ct = default);

        /// <summary>NUEVO para el rediseño: gráfica "Últimos 7 días". Incluye los días sin vistas (en 0).</summary>
        Task<List<VistasDia>> ListarVistasPorDiaAsync(int eventoId, int dias = 7, CancellationToken ct = default);

        /// <summary>"Enviar a revisión" o, con <paramref name="enviarARevision"/> = false, "Guardar borrador".</summary>
        /// <returns>Id del evento nuevo.</returns>
        Task<int> CrearAsync(int organizadorId, DatosEvento datos, bool enviarARevision = true, CancellationToken ct = default);

        /// <summary>Edita un evento propio. Si estaba aprobado, vuelve a moderación y sale del carrusel.</summary>
        /// <returns>false si no existe, no es suyo o está cancelado.</returns>
        Task<bool> ActualizarAsync(int eventoId, int organizadorId, DatosEvento datos, CancellationToken ct = default);

        /// <summary>Manda a moderación un borrador o un evento rechazado ya corregido.</summary>
        Task<bool> EnviarARevisionAsync(int eventoId, int organizadorId, CancellationToken ct = default);

        /// <summary>Modal "Cancelar evento". El motivo se muestra a los inscritos.</summary>
        Task<bool> CancelarAsync(int eventoId, int organizadorId, string motivo, CancellationToken ct = default);

        /// <summary>Agrega una foto a la galería. Si el evento estaba aprobado, vuelve a moderación.</summary>
        /// <returns>Id de la imagen.</returns>
        Task<int> AgregarImagenAsync(int eventoId, string url, CancellationToken ct = default);
    }
}
