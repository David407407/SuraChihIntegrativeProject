using BackendLogica.Modelos;

namespace BackendLogica.Contratos
{
    /// <summary>
    /// Corazón de las tarjetas y pantalla "Mis favoritos" (pestañas Todos / Eventos / Lugares).
    /// Implementaciones: <c>FavoritoDB</c> y <c>FalsoFavoriteRepository</c>.
    /// </summary>
    /// <remarks>
    /// Los favoritos de LUGARES salen en el diseño pero la BD v1 solo tiene <c>favorite</c> para eventos:
    /// en <c>FavoritoDB</c> esos métodos lanzan <see cref="Datos.PendienteBDException"/>; el falso sí funcionan.
    /// </remarks>
    public interface IFavoriteRepository
    {
        // --- Eventos ---------------------------------------------------------------

        /// <summary>Marca el evento como favorito. Repetirlo no hace nada.</summary>
        Task<int> AgregarAsync(int usuarioId, int eventoId, CancellationToken ct = default);
        Task<int> QuitarAsync(int usuarioId, int eventoId, CancellationToken ct = default);

        /// <summary>Lo que hace el corazón: si ya era favorito lo quita, si no lo agrega.</summary>
        /// <returns>true si quedó como favorito.</returns>
        Task<bool> AlternarAsync(int usuarioId, int eventoId, CancellationToken ct = default);

        /// <summary>Ids de eventos favoritos, para pintar los corazones llenos en cualquier lista.</summary>
        Task<HashSet<int>> ListarIdsAsync(int usuarioId, CancellationToken ct = default);

        /// <summary>"Mis favoritos" (eventos): los más recientes primero, incluye los que ya terminaron.</summary>
        Task<List<EventoTarjeta>> ListarAsync(int usuarioId, CancellationToken ct = default);

        // --- Lugares (NUEVO para el rediseño; pendiente en la BD v1) ---------------

        Task<bool> AlternarLugarAsync(int usuarioId, int lugarId, CancellationToken ct = default);
        Task<HashSet<int>> ListarIdsLugaresAsync(int usuarioId, CancellationToken ct = default);

        /// <summary>"Mis favoritos" (lugares aprobados): los más recientes primero.</summary>
        Task<List<LugarTarjeta>> ListarLugaresAsync(int usuarioId, CancellationToken ct = default);
    }
}
