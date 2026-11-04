using BackendLogica.Contratos;
using BackendLogica.Modelos;

namespace Interfaz.Servicios
{
    /// <summary>"Modal / Reportar": cualquier usuario con sesión reporta un evento, lugar o reseña (tabla <c>report</c>).</summary>
    public sealed class ServicioReportes(ISuraChihBD bd, Sesion sesion)
    {
        /// <summary>Con motivo <see cref="MotivoReporte.Otro"/> los detalles son obligatorios (lo revisa BackendLogica).</summary>
        public Task EnviarAsync(TipoObjetivoReporte tipo, int objetivoId, MotivoReporte motivo, string? detalles) =>
            bd.Reportes.CrearAsync(sesion.Requerir("reportar").Id, tipo, objetivoId, motivo, detalles);
    }
}
