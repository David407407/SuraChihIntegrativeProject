using BackendLogica.Contratos;
using BackendLogica.Modelos;

namespace Interfaz.Servicios
{
    /// <summary>
    /// Corazón de las tarjetas y del detalle (Componentes/Base/BotonFavorito.razor) y pantalla
    /// "Mis favoritos" (Paginas/Favoritos.razor). Tablas <c>favorite</c> y <c>place_favorite</c>.
    /// </summary>
    public sealed class ServicioFavoritos(ISuraChihBD bd, Sesion sesion)
    {
        /// <summary>
        /// Un corazón cambió y BackendLogica ya lo guardó. Mis favoritos lo usa para quitar la tarjeta.
        /// </summary>
        public event Action<TipoFavorito, int, bool>? Guardado;

        /// <returns>true si quedó en favoritos.</returns>
        public async Task<bool> AlternarAsync(TipoFavorito tipo, int id)
        {
            Usuario usuario = sesion.Requerir("guardar tus favoritos");
            bool activo = tipo == TipoFavorito.Evento
                ? await bd.Favoritos.AlternarAsync(usuario.Id, id)
                : await bd.Favoritos.AlternarLugarAsync(usuario.Id, id);
            Guardado?.Invoke(tipo, id, activo);
            return activo;
        }

        /// <summary>Eventos (incluye los que ya terminaron) y lugares aprobados, los más recientes primero.</summary>
        public async Task<DatosFavoritos> ListarAsync()
        {
            Usuario usuario = sesion.Requerir("ver tus favoritos");
            var eventos = bd.Favoritos.ListarAsync(usuario.Id);
            var lugares = bd.Favoritos.ListarLugaresAsync(usuario.Id);
            await Task.WhenAll(eventos, lugares);
            return new DatosFavoritos(eventos.Result, lugares.Result);
        }
    }

    public enum TipoFavorito { Evento, Lugar }

    public sealed record DatosFavoritos(
        IReadOnlyList<EventoTarjeta> Eventos,
        IReadOnlyList<LugarTarjeta> Lugares);
}
