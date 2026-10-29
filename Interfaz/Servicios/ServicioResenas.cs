using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;
using Interfaz.Utilidades;

namespace Interfaz.Servicios
{
    /// <summary>
    /// Pantalla "Reseñas" (Paginas/Resenas.razor) y modal "Escribe una reseña" (ModalEscribirResena), para
    /// eventos (<c>event_review</c>) y lugares (<c>place_review</c>). El promedio de un lugar sale de
    /// <c>v_place_rating</c>.
    /// </summary>
    /// <remarks>
    /// Reglas de los triggers: un evento solo se reseña cuando ya terminó, nadie reseña lo propio y hay una
    /// reseña por usuario. Si el usuario ya tiene la suya, la modal la edita en vez de crear otra.
    /// </remarks>
    public sealed class ServicioResenas(ISuraChihBD bd, Sesion sesion)
    {
        public const int MaximoComentario = 1000;

        /// <summary>Se publicó o editó una reseña (las pantallas abiertas se recargan).</summary>
        public event Action<TipoResena, int>? Publicada;

        public async Task<DatosResenas> ObtenerAsync(TipoResena tipo, int objetivoId)
        {
            var (titulo, subtitulo, puedeEscribir) = await DescribirAsync(tipo, objetivoId);
            var lista = bd.Resenas.ListarAsync(tipo, objetivoId);
            var resumen = bd.Resenas.ObtenerResumenAsync(tipo, objetivoId);
            await Task.WhenAll(lista, resumen);
            return new DatosResenas(tipo, objetivoId, titulo, subtitulo, puedeEscribir, resumen.Result, lista.Result);
        }

        /// <summary>La reseña del usuario sobre ese evento o lugar, o null (sin sesión también).</summary>
        public async Task<Resena?> ObtenerMiaAsync(TipoResena tipo, int objetivoId)
        {
            if (sesion.Usuario is not { } usuario) return null;
            var todas = await bd.Resenas.ListarAsync(tipo, objetivoId, incluirOcultas: true);
            return todas.FirstOrDefault(r => r.UsuarioId == usuario.Id);
        }

        /// <summary>"Publicar reseña": crea la reseña o, si <paramref name="resenaId"/> viene, edita la suya.</summary>
        public async Task GuardarAsync(TipoResena tipo, int objetivoId, int estrellas, string? comentario, int? resenaId)
        {
            Usuario usuario = sesion.Requerir("escribir reseñas");
            if (estrellas is < 1 or > 5) throw new DatosInvalidosException("Elige de 1 a 5 estrellas.");
            comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
            if (comentario?.Length > MaximoComentario)
                throw new DatosInvalidosException($"La reseña puede tener hasta {MaximoComentario} caracteres.");

            if (resenaId is { } id)
            {
                if (!await bd.Resenas.EditarAsync(tipo, id, usuario.Id, estrellas, comentario))
                    throw new ReglaNegocioException("No se pudo editar tu reseña. Intenta de nuevo.");
            }
            else
            {
                await bd.Resenas.CrearAsync(tipo, objetivoId, usuario.Id, estrellas, comentario);
            }
            Publicada?.Invoke(tipo, objetivoId);
        }

        /// <summary>Título y subtítulo de la pantalla ("Las Ligas Menores · Foro Indie Rocks!").</summary>
        private async Task<(string Titulo, string Subtitulo, bool PuedeEscribir)> DescribirAsync(TipoResena tipo, int id)
        {
            if (tipo == TipoResena.Evento)
            {
                EventoTarjeta e = (await bd.Eventos.ObtenerAsync(id))?.Evento
                    ?? throw new ReglaNegocioException("Este evento ya no existe.");
                if (e.Estado is not (EstadoEvento.Aprobado or EstadoEvento.Cancelado))
                    throw new ReglaNegocioException("Este evento no está disponible.");
                string? lugar = Formato.UbicacionEvento(e);
                // Un evento solo se reseña cuando ya terminó (trigger de event_review).
                return (e.Titulo, lugar is { Length: > 0 } ? $"{e.Titulo} · {lugar}" : e.Titulo, e.Terminado);
            }

            LugarTarjeta l = (await bd.Lugares.ObtenerAsync(id))?.Lugar
                ?? throw new ReglaNegocioException("Este lugar ya no existe.");
            if (l.Estado != EstadoLugar.Aprobado) throw new ReglaNegocioException("Este lugar no está disponible.");
            return (l.Nombre, $"{l.Nombre} · {l.Direccion}", true);
        }
    }

    /// <param name="Titulo">Nombre del evento o lugar (subtítulo de la modal "Escribe una reseña").</param>
    /// <param name="Subtitulo">"Nombre · Lugar" bajo el título de la pantalla.</param>
    /// <param name="PuedeEscribir">Los eventos solo se reseñan cuando ya terminaron.</param>
    /// <param name="Resumen">Promedio, total y barras por estrellas.</param>
    /// <param name="Lista">Reseñas visibles, las más recientes primero.</param>
    public sealed record DatosResenas(
        TipoResena Tipo,
        int ObjetivoId,
        string Titulo,
        string Subtitulo,
        bool PuedeEscribir,
        ResumenResenas Resumen,
        IReadOnlyList<Resena> Lista);
}
