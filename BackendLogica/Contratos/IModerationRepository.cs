using BackendLogica.Modelos;

namespace BackendLogica.Contratos
{
    /// <summary>
    /// Pantallas de moderación: aprobar / rechazar, historial y carrusel destacado.
    /// Lo pendiente se lista con <c>ListarPorEstadoAsync</c> de eventos, lugares y organizadores.
    /// Implementaciones: <c>ModeracionDB</c> y <c>FalsoModerationRepository</c>.
    /// </summary>
    public interface IModerationRepository
    {
        /// <summary>Aprueba o rechaza. Rechazar exige <paramref name="comentario"/> (se le muestra al autor).</summary>
        /// <returns>Id del registro de moderación.</returns>
        /// <exception cref="Datos.ReglaNegocioException">No es moderador, el objetivo no está pendiente o es suyo.</exception>
        Task<int> DecidirAsync(int moderadorId, TipoObjetivoModeracion tipo, int objetivoId,
            DecisionModeracion decision, string? comentario = null, CancellationToken ct = default);

        Task<int> AprobarAsync(int moderadorId, TipoObjetivoModeracion tipo, int objetivoId, string? comentario = null, CancellationToken ct = default);
        Task<int> RechazarAsync(int moderadorId, TipoObjetivoModeracion tipo, int objetivoId, string motivo, CancellationToken ct = default);

        /// <summary>"Moderación / Historial": las decisiones más recientes primero.</summary>
        Task<List<RegistroModeracion>> ListarHistorialAsync(int limite = 50, CancellationToken ct = default);

        /// <summary>
        /// NUEVO para el rediseño: "Ver motivo" del panel del organizador (última decisión sobre ese objetivo).
        /// </summary>
        Task<RegistroModeracion?> ObtenerUltimaDecisionAsync(TipoObjetivoModeracion tipo, int objetivoId, CancellationToken ct = default);

        /// <summary>Pone (o quita, con <paramref name="orden"/> null) un evento aprobado en el carrusel.</summary>
        Task<bool> DestacarEventoAsync(int eventoId, int? orden, CancellationToken ct = default);
    }
}
