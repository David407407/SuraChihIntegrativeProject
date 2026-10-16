using BackendLogica.Configuracion;
using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace BackendLogica.Repositorios
{
    /// <summary>
    /// Favoritos ("me interesa", el corazón). Priorizan el evento en el inicio del usuario y
    /// cuentan como "interesados" en el panel del organizador.
    /// </summary>
    public sealed class FavoritoDB : ConsultasEventoDB, IFavoriteRepository
    {
        public FavoritoDB(ConfiguracionBD? configuracion = null) : base(configuracion) { }

        /// <summary>Marca el evento como favorito. Repetirlo no hace nada.</summary>
        public Task<int> AgregarAsync(int usuarioId, int eventoId, CancellationToken ct = default) =>
            EjecutarAsync("INSERT IGNORE INTO favorite (user_id, event_id) VALUES (@usuarioId, @eventoId)", new { usuarioId, eventoId }, ct);

        public Task<int> QuitarAsync(int usuarioId, int eventoId, CancellationToken ct = default) =>
            EjecutarAsync("DELETE FROM favorite WHERE user_id = @usuarioId AND event_id = @eventoId", new { usuarioId, eventoId }, ct);

        /// <summary>Lo que hace el botón de corazón: si ya era favorito lo quita, si no lo agrega.</summary>
        /// <returns>true si quedó como favorito.</returns>
        public Task<bool> AlternarAsync(int usuarioId, int eventoId, CancellationToken ct = default) =>
            EnTransaccionAsync(async tx =>
            {
                int borradas = await tx.EjecutarAsync(
                    "DELETE FROM favorite WHERE user_id = @usuarioId AND event_id = @eventoId", new { usuarioId, eventoId });
                if (borradas > 0) return false;

                await tx.EjecutarAsync("INSERT INTO favorite (user_id, event_id) VALUES (@usuarioId, @eventoId)", new { usuarioId, eventoId });
                return true;
            }, ct);

        /// <summary>Ids de los eventos favoritos, para pintar los corazones llenos en cualquier lista.</summary>
        public async Task<HashSet<int>> ListarIdsAsync(int usuarioId, CancellationToken ct = default) =>
            (await ConsultarAsync("SELECT event_id FROM favorite WHERE user_id = @usuarioId",
                r => r.Entero("event_id"), new { usuarioId }, ct)).ToHashSet();

        /// <summary>Pantalla "Mis favoritos": los más recientes primero (incluye los que ya terminaron).</summary>
        public Task<List<EventoTarjeta>> ListarAsync(int usuarioId, CancellationToken ct = default) =>
            ConsultarTarjetasAsync($"""
                {SelectTarjeta}
                JOIN favorite fav ON fav.event_id = c.id AND fav.user_id = @usuarioId
                WHERE c.status = 'approved'
                ORDER BY fav.created_at DESC
                """, new { usuarioId }, ct);

        // ------------------------------------------------------------------
        // Lugares (tabla place_favorite)
        // ------------------------------------------------------------------

        /// <summary>Corazón de una tarjeta de lugar: si ya era favorito lo quita, si no lo agrega.</summary>
        /// <returns>true si quedó como favorito.</returns>
        public Task<bool> AlternarLugarAsync(int usuarioId, int lugarId, CancellationToken ct = default) =>
            EnTransaccionAsync(async tx =>
            {
                int borradas = await tx.EjecutarAsync(
                    "DELETE FROM place_favorite WHERE user_id = @usuarioId AND place_id = @lugarId", new { usuarioId, lugarId });
                if (borradas > 0) return false;

                await tx.EjecutarAsync("INSERT INTO place_favorite (user_id, place_id) VALUES (@usuarioId, @lugarId)", new { usuarioId, lugarId });
                return true;
            }, ct);

        /// <summary>Ids de los lugares favoritos, para pintar los corazones llenos.</summary>
        public async Task<HashSet<int>> ListarIdsLugaresAsync(int usuarioId, CancellationToken ct = default) =>
            (await ConsultarAsync("SELECT place_id FROM place_favorite WHERE user_id = @usuarioId",
                r => r.Entero("place_id"), new { usuarioId }, ct)).ToHashSet();

        /// <summary>"Mis favoritos" (lugares aprobados): los más recientes primero.</summary>
        public Task<List<LugarTarjeta>> ListarLugaresAsync(int usuarioId, CancellationToken ct = default) =>
            new LugarDB(Configuracion).ListarFavoritosAsync(usuarioId, ct);
    }
}
