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

        /// <summary>"Moderación / Historial": las decisiones más recientes primero. Sin filtro (null) trae todas.</summary>
        Task<List<RegistroModeracion>> ListarHistorialAsync(int limite = 50, DecisionModeracion? decision = null, TipoObjetivoModeracion? tipo = null, CancellationToken ct = default);

        /// <summary>Total del historial con esos filtros ("Mostrando 7 de 214").</summary>
        Task<int> ContarHistorialAsync(DecisionModeracion? decision = null, TipoObjetivoModeracion? tipo = null, CancellationToken ct = default);

        /// <summary>
        /// Reseñas ocultadas o vueltas a mostrar, las más recientes primero.
        /// Un supermoderador ve todas; un moderador, solo las acciones suyas.
        /// </summary>
        /// <exception cref="Datos.ReglaNegocioException">No es moderador.</exception>
        Task<List<RegistroResena>> ListarHistorialResenasAsync(int moderadorId, int limite = 50, CancellationToken ct = default);

        /// <summary>Total de lo que ese moderador puede ver en el historial de reseñas.</summary>
        /// <exception cref="Datos.ReglaNegocioException">No es moderador.</exception>
        Task<int> ContarHistorialResenasAsync(int moderadorId, CancellationToken ct = default);

        /// <summary>
        /// NUEVO para el rediseño: "Ver motivo" del panel del organizador (última decisión sobre ese objetivo).
        /// </summary>
        Task<RegistroModeracion?> ObtenerUltimaDecisionAsync(TipoObjetivoModeracion tipo, int objetivoId, CancellationToken ct = default);

        /// <summary>
        /// NUEVO para el rediseño: cuándo se mandó a revisión cada evento y lugar pendiente ("Hace 2 días",
        /// "Enviado el 7 oct a las 18:42"). Es su última modificación (<c>updated_at</c>).
        /// </summary>
        Task<List<EnvioPendiente>> ListarEnviosPendientesAsync(CancellationToken ct = default);

        /// <summary>Pone (o quita, con <paramref name="orden"/> null) un evento aprobado en el carrusel.</summary>
        Task<bool> DestacarEventoAsync(int eventoId, int? orden, CancellationToken ct = default);
    }
}
