using BackendLogica.Modelos;

namespace BackendLogica.Contratos
{
    /// <summary>
    /// Modal "Reportar" y pantalla "Moderación / Reportes".
    /// Implementaciones: <c>ReporteDB</c> y <c>FalsoReportRepository</c>.
    /// </summary>
    public interface IReportRepository
    {
        /// <summary>Envía un reporte. Con motivo <see cref="MotivoReporte.Otro"/> los detalles son obligatorios.</summary>
        /// <returns>Id del reporte.</returns>
        Task<int> CrearAsync(int reportanteId, TipoObjetivoReporte tipo, int objetivoId, MotivoReporte motivo, string? detalles, CancellationToken ct = default);

        /// <summary>Bandeja de reportes (por defecto los abiertos, del más antiguo al más nuevo).</summary>
        Task<List<Reporte>> ListarAsync(EstadoReporte estado = EstadoReporte.Abierto, CancellationToken ct = default);

        /// <summary>"Moderación / Reportes": cada reporte con su evento o lugar. Abiertos: el que lleva más tiempo esperando primero. Cerrados: el último en cerrarse primero.</summary>
        Task<List<ReporteDetalle>> ListarDetalleAsync(EstadoReporte estado = EstadoReporte.Abierto, CancellationToken ct = default);

        /// <summary>Cierra un reporte como resuelto o descartado.</summary>
        /// <exception cref="Datos.ReglaNegocioException">No es moderador.</exception>
        Task<bool> ResolverAsync(int reporteId, int moderadorId, EstadoReporte resultado, CancellationToken ct = default);

        /// <summary>Vuelve a abrir un reporte cerrado (por si se cerró por error).</summary>
        /// <returns><c>false</c> si no existe o ya estaba abierto.</returns>
        /// <exception cref="Datos.ReglaNegocioException">No es moderador.</exception>
        Task<bool> ReabrirAsync(int reporteId, int moderadorId, CancellationToken ct = default);
    }
}
