using BackendLogica.Modelos;

namespace BackendLogica.Contratos
{
    /// <summary>
    /// "Voy a ir" / "Cancelar inscripción", "Mis planes" y la tabla de inscritos del organizador.
    /// No reserva cupo: los boletos se compran en la página oficial.
    /// Implementaciones: <c>InscripcionDB</c> y <c>FalsoInscriptionRepository</c>.
    /// </summary>
    public interface IInscriptionRepository
    {
        /// <summary>
        /// Inscribe al usuario (o reactiva una inscripción cancelada). Eventos no aprobados o terminados
        /// se rechazan con <see cref="Datos.ReglaNegocioException"/> ("No puedes inscribirte a este evento.").
        /// </summary>
        Task<int> InscribirAsync(int usuarioId, int eventoId, CancellationToken ct = default);

        /// <returns>false si no estaba inscrito.</returns>
        Task<bool> CancelarAsync(int usuarioId, int eventoId, CancellationToken ct = default);

        Task<bool> EstaInscritoAsync(int usuarioId, int eventoId, CancellationToken ct = default);

        /// <summary>Pantalla "Mis planes".</summary>
        Task<List<PlanUsuario>> ListarPlanesAsync(int usuarioId, PestanaPlanes pestana, CancellationToken ct = default);

        /// <summary>
        /// NUEVO para el rediseño: tabla "Inscritos" de las estadísticas del evento, en orden de inscripción.
        /// Solo devuelve filas si el evento es de <paramref name="organizadorId"/>.
        /// </summary>
        Task<List<Inscrito>> ListarInscritosAsync(int eventoId, int organizadorId, CancellationToken ct = default);
    }
}
