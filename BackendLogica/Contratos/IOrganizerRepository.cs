using BackendLogica.Modelos;

namespace BackendLogica.Contratos
{
    /// <summary>
    /// "Hazte organizador" y el encabezado del panel del organizador.
    /// Implementaciones: <c>OrganizadorDB</c> y <c>FalsoOrganizerRepository</c>.
    /// </summary>
    public interface IOrganizerRepository
    {
        /// <summary>Envía la solicitud. Si una anterior fue rechazada, se reenvía con los datos nuevos.</summary>
        /// <exception cref="Datos.DuplicadoException">Ya tiene una solicitud pendiente o ya es organizador.</exception>
        Task SolicitarAsync(SolicitudOrganizador s, CancellationToken ct = default);

        Task<PerfilOrganizador?> ObtenerAsync(int usuarioId, CancellationToken ct = default);

        /// <summary>Puede publicar eventos y registrar lugares.</summary>
        Task<bool> EsAprobadoAsync(int usuarioId, CancellationToken ct = default);

        /// <summary>Solicitudes en un estado (Pendiente para "Moderación / Organizadores").</summary>
        Task<List<PerfilOrganizador>> ListarPorEstadoAsync(EstadoOrganizador estado, CancellationToken ct = default);

        /// <summary>Edita los datos públicos del organizador (no cambia su estado).</summary>
        Task<bool> ActualizarPerfilAsync(int usuarioId, string nombreNegocio, string? descripcion, string whatsApp, CancellationToken ct = default);

        /// <summary>Badge de verificado (moderadores).</summary>
        Task<bool> MarcarVerificadoAsync(int usuarioId, bool verificado, CancellationToken ct = default);
    }
}
