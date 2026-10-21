using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace Interfaz.Servicios
{
    /// <summary>
    /// Datos de la hoja "Lugar / ..." de Figma (Componentes/Detalle/DetalleLugar.razor). Se abre al elegir
    /// un pin en la pantalla Mapa y con "Ver detalles" de las Card lugar.
    /// </summary>
    public sealed class ServicioLugar(ISuraChihBD bd, Sesion sesion)
    {
        /// <summary>Opiniones que caben en la hoja (Figma muestra dos; las demás van en la pantalla "Reseñas").</summary>
        private const int Opiniones = 2;
        /// <summary>Tarjetas de "También te puede gustar" (una fila de tres).</summary>
        private const int Recomendados = 3;

        public async Task<DatosDetalleLugar> ObtenerAsync(int lugarId)
        {
            LugarDetalle detalle = await bd.Lugares.ObtenerAsync(lugarId)
                ?? throw new ReglaNegocioException("Este lugar ya no existe.");
            LugarTarjeta lugar = detalle.Lugar;
            Usuario? usuario = sesion.Usuario;

            // Aprobados los ve cualquiera; pendientes, rechazados e inactivos solo su dueño o un moderador.
            bool puedeVerlo = lugar.Estado == EstadoLugar.Aprobado
                || (usuario != null && (usuario.EsModerador || usuario.Id == lugar.DuenoId));
            if (!puedeVerlo) throw new ReglaNegocioException("Este lugar no está disponible.");

            var resenas = bd.Resenas.ListarAsync(TipoResena.Lugar, lugarId);
            var recomendados = ListarRecomendadosAsync(lugar);
            var favoritos = usuario != null ? bd.Favoritos.ListarIdsLugaresAsync(usuario.Id) : Task.FromResult(new HashSet<int>());
            await Task.WhenAll(resenas, recomendados, favoritos);

            // Primero las que tienen comentario: en la hoja solo caben dos.
            var opiniones = resenas.Result
                .OrderByDescending(r => !string.IsNullOrWhiteSpace(r.Comentario))
                .Take(Opiniones)
                .ToList();

            return new DatosDetalleLugar(
                lugar,
                detalle.Galeria,
                favoritos.Result.Contains(lugarId),
                opiniones,
                recomendados.Result,
                favoritos.Result);
        }

        /// <summary>"También te puede gustar": aprobados del mismo tipo y, si no alcanzan, los mejor calificados.</summary>
        private async Task<List<LugarTarjeta>> ListarRecomendadosAsync(LugarTarjeta lugar)
        {
            int? tipo = lugar.Etiquetas.FirstOrDefault()?.Id;
            var mismoTipo = tipo != null ? bd.Lugares.ListarAprobadosAsync(tipo) : Task.FromResult(new List<LugarTarjeta>());
            var mejores = bd.Lugares.ListarRecomendadosAsync(limite: Recomendados + 1);
            await Task.WhenAll(mismoTipo, mejores);

            return mismoTipo.Result.Concat(mejores.Result)
                .Where(l => l.Id != lugar.Id)
                .DistinctBy(l => l.Id)
                .Take(Recomendados)
                .ToList();
        }

        /// <summary>
        /// Pantalla Mapa: todos los lugares aprobados con sus coordenadas. Son pocos, así que el buscador y
        /// las píldoras de tipo (Cafés, Museos, Parques) filtran en memoria sin volver a consultar.
        /// </summary>
        public Task<List<LugarTarjeta>> ListarParaMapaAsync() => bd.Lugares.ListarAprobadosAsync();
    }

    /// <param name="Favorito">Corazón lleno en la portada (tabla place_favorite).</param>
    /// <param name="Opiniones">Las reseñas visibles más recientes (hasta 2), primero las que traen comentario.</param>
    /// <param name="Recomendados">"También te puede gustar" (hasta 3).</param>
    /// <param name="Favoritos">Ids de lugares favoritos del usuario, para los corazones de las recomendaciones.</param>
    public sealed record DatosDetalleLugar(
        LugarTarjeta Lugar,
        IReadOnlyList<string> Galeria,
        bool Favorito,
        IReadOnlyList<Resena> Opiniones,
        IReadOnlyList<LugarTarjeta> Recomendados,
        IReadOnlySet<int> Favoritos);
}
