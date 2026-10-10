using BackendLogica.Modelos;

namespace BackendLogica.Contratos
{
    /// <summary>
    /// Pantalla "Reseñas", modal "Escribir reseña" y "Opiniones" del detalle del lugar.
    /// Implementaciones: <c>ResenaDB</c> y <c>FalsoReviewRepository</c>.
    /// </summary>
    /// <remarks>
    /// Reglas de los triggers: un evento solo se reseña cuando ya terminó, nadie reseña lo propio,
    /// una reseña por usuario (<see cref="Datos.DuplicadoException"/>).
    /// </remarks>
    public interface IReviewRepository
    {
        /// <returns>Id de la reseña nueva.</returns>
        Task<int> CrearAsync(TipoResena tipo, int objetivoId, int usuarioId, int calificacion, string? comentario, CancellationToken ct = default);

        /// <summary>El autor edita su reseña.</summary>
        Task<bool> EditarAsync(TipoResena tipo, int resenaId, int usuarioId, int calificacion, string? comentario, CancellationToken ct = default);

        /// <summary>El organizador / dueño responde públicamente (solo sobre lo que es suyo).</summary>
        Task<bool> ResponderAsync(TipoResena tipo, int resenaId, int duenoId, string respuesta, CancellationToken ct = default);

        /// <summary>Reseñas de un evento o lugar, las más recientes primero.</summary>
        Task<List<Resena>> ListarAsync(TipoResena tipo, int objetivoId, bool incluirOcultas = false, CancellationToken ct = default);

        /// <summary>NUEVO para el rediseño: promedio, total y barras por estrellas de la pantalla "Reseñas".</summary>
        Task<ResumenResenas> ObtenerResumenAsync(TipoResena tipo, int objetivoId, CancellationToken ct = default);

        /// <summary>Un moderador oculta (o vuelve a mostrar) una reseña tras un reporte.</summary>
        Task<bool> CambiarVisibilidadAsync(TipoResena tipo, int resenaId, bool oculta, CancellationToken ct = default);
    }
}
