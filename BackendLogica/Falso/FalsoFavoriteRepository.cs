using BackendLogica.Contratos;
using BackendLogica.Modelos;

namespace BackendLogica.Falso
{
    /// <summary>
    /// <see cref="IFavoriteRepository"/> en memoria. Aquí SÍ funcionan los favoritos de lugares
    /// (la BD v1 todavía no los tiene).
    /// </summary>
    public sealed class FalsoFavoriteRepository : RepositorioFalso, IFavoriteRepository
    {
        internal FalsoFavoriteRepository(AlmacenFalso almacen) : base(almacen) { }

        // --- Eventos ---------------------------------------------------------------

        /// <summary>INSERT IGNORE: 1 si se agregó, 0 si ya estaba (o el evento no existe).</summary>
        public Task<int> AgregarAsync(int usuarioId, int eventoId, CancellationToken ct = default) =>
            Hacer(a =>
            {
                if (a.Favoritos.Any(f => f.UsuarioId == usuarioId && f.EventoId == eventoId)
                    || a.Usuario(usuarioId) == null || a.Evento(eventoId) == null) return 0;
                a.Favoritos.Add(new FilaFavorito(usuarioId, eventoId, Ahora));
                return 1;
            }, ct);

        public Task<int> QuitarAsync(int usuarioId, int eventoId, CancellationToken ct = default) =>
            Hacer(a => a.Favoritos.RemoveAll(f => f.UsuarioId == usuarioId && f.EventoId == eventoId), ct);

        public Task<bool> AlternarAsync(int usuarioId, int eventoId, CancellationToken ct = default) =>
            Hacer(a =>
            {
                if (a.Favoritos.RemoveAll(f => f.UsuarioId == usuarioId && f.EventoId == eventoId) > 0) return false;
                if (a.Usuario(usuarioId) == null || a.Evento(eventoId) == null) throw AlmacenFalso.NoExiste();
                a.Favoritos.Add(new FilaFavorito(usuarioId, eventoId, Ahora));
                return true;
            }, ct);

        public Task<HashSet<int>> ListarIdsAsync(int usuarioId, CancellationToken ct = default) =>
            Hacer(a => a.Favoritos.Where(f => f.UsuarioId == usuarioId).Select(f => f.EventoId).ToHashSet(), ct);

        public Task<List<EventoTarjeta>> ListarAsync(int usuarioId, CancellationToken ct = default) =>
            Hacer(a => a.Favoritos
                .Where(f => f.UsuarioId == usuarioId)
                .OrderByDescending(f => f.CreadoEn)
                .Select(f => a.Evento(f.EventoId)!)
                .Where(e => e.Estado == EstadoEvento.Aprobado)
                .Select(e => a.Tarjeta(e, Ahora))
                .ToList(), ct);

        // --- Lugares ---------------------------------------------------------------

        public Task<bool> AlternarLugarAsync(int usuarioId, int lugarId, CancellationToken ct = default) =>
            Hacer(a =>
            {
                if (a.FavoritosLugares.RemoveAll(f => f.UsuarioId == usuarioId && f.LugarId == lugarId) > 0) return false;
                if (a.Usuario(usuarioId) == null || a.Lugar(lugarId) == null) throw AlmacenFalso.NoExiste();
                a.FavoritosLugares.Add(new FilaFavoritoLugar(usuarioId, lugarId, Ahora));
                return true;
            }, ct);

        public Task<HashSet<int>> ListarIdsLugaresAsync(int usuarioId, CancellationToken ct = default) =>
            Hacer(a => a.FavoritosLugares.Where(f => f.UsuarioId == usuarioId).Select(f => f.LugarId).ToHashSet(), ct);

        public Task<List<LugarTarjeta>> ListarLugaresAsync(int usuarioId, CancellationToken ct = default) =>
            Hacer(a => a.FavoritosLugares
                .Where(f => f.UsuarioId == usuarioId)
                .OrderByDescending(f => f.CreadoEn)
                .Select(f => a.Lugar(f.LugarId)!)
                .Where(l => l.Estado == EstadoLugar.Aprobado)
                .Select(l => a.Tarjeta(l, Ahora))
                .ToList(), ct);
    }
}
