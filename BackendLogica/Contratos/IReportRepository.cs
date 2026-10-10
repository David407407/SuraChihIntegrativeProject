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

        /// <summary>Cierra un reporte como resuelto o descartado.</summary>
        Task<bool> ResolverAsync(int reporteId, int moderadorId, EstadoReporte resultado, CancellationToken ct = default);
    }
}
