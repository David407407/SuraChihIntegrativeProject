using BackendLogica.Contratos;
using BackendLogica.Modelos;
using SuraChihIntegrativeProject.Puente;
using SuraChihIntegrativeProject.Servicios;

namespace SuraChihIntegrativeProject.Controladores
{
    /// <summary>
    /// Acción del Home (wwwroot/js/home.js). Todas sus secciones en una sola llamada; las consultas
    /// corren en paralelo. Con sesión, "Te podrían interesar" pone primero los favoritos del usuario
    /// (consulta de referencia «Inicio del usuario» del SQL) y los corazones salen llenos.
    /// </summary>
    public sealed class ControladorHome
    {
        private const int PorFila = 10;

        // Las secciones se arman por tag.icon y no por id, para no depender del orden en que se
        // insertaron las etiquetas en la BD.
        /// <summary>Tiles de "Explora por categoría", en el orden de Figma.</summary>
        private static readonly string[] IconosCategorias = ["music", "family", "run", "robot", "food", "coffee", "museum"];
        private static readonly string[] IconosMusica = ["music"];
        /// <summary>"Actividades y talleres": todo lo que no es un concierto.</summary>
        private static readonly string[] IconosActividades = ["family", "run", "robot", "food"];
        private const string IconoCafe = "coffee";
        /// <summary>"Para pasar la tarde": museos, parques y foros.</summary>
        private static readonly string[] IconosParaLaTarde = ["museum", "tree", "stage"];

        private readonly ISuraChihBD _bd;
        private readonly Sesion _sesion;

        public ControladorHome(ISuraChihBD bd, Sesion sesion)
        {
            _bd = bd;
            _sesion = sesion;
        }

        public void Registrar(PuenteWeb puente)
        {
            puente.Registrar("home.obtener", async () => await ObtenerAsync());
        }

        private async Task<DatosHome> ObtenerAsync()
        {
            int? usuarioId = _sesion.Usuario?.Id;
            List<Etiqueta> etiquetas = await _bd.Etiquetas.ListarAsync();
            int[] Ids(IEnumerable<string> iconos) => etiquetas.Where(t => iconos.Contains(t.Icono)).Select(t => t.Id).ToArray();

            var destacados = _bd.Eventos.ListarDestacadosAsync(limite: 4);
            var masGuardados = _bd.Eventos.ListarMasGuardadosAsync(limite: 6);
            var lugaresRecomendados = _bd.Lugares.ListarRecomendadosAsync(limite: 3);
            var tePodrianInteresar = _bd.Eventos.ListarProximosAsync(usuarioId, limite: PorFila);
            var musica = PorEtiquetasAsync(Ids(IconosMusica));
            var actividades = PorEtiquetasAsync(Ids(IconosActividades));
            var lugares = _bd.Lugares.ListarAprobadosAsync();
            var favoritosEventos = usuarioId is int id ? _bd.Favoritos.ListarIdsAsync(id) : Task.FromResult(new HashSet<int>());
            var favoritosLugares = usuarioId is int id2 ? _bd.Favoritos.ListarIdsLugaresAsync(id2) : Task.FromResult(new HashSet<int>());
            await Task.WhenAll(destacados, masGuardados, lugaresRecomendados, tePodrianInteresar, musica, actividades,
                lugares, favoritosEventos, favoritosLugares);

            var eventosPopulares = destacados.Result.Concat(masGuardados.Result).DistinctBy(e => e.Id).ToList();
            var mejoresLugares = lugaresRecomendados.Result;

            return new DatosHome(
                UsuarioId: usuarioId,
                Carrusel: ArmarCarrusel(eventosPopulares, mejoresLugares),
                Recomendados: ArmarRecomendados(masGuardados.Result, mejoresLugares),
                Categorias: IconosCategorias
                    .Select(icono => etiquetas.FirstOrDefault(t => t.Icono == icono))
                    .OfType<Etiqueta>().ToList(),
                TePodrianInteresar: tePodrianInteresar.Result,
                Musica: musica.Result,
                Actividades: actividades.Result,
                Cafes: Lugares(lugares.Result, [IconoCafe]),
                ParaLaTarde: Lugares(lugares.Result, IconosParaLaTarde),
                FavoritosEventos: favoritosEventos.Result,
                FavoritosLugares: favoritosLugares.Result);
        }

        /// <summary>Eventos vigentes con AL MENOS UNA de las etiquetas. Sin etiquetas en la BD, ninguno.</summary>
        private Task<List<EventoTarjeta>> PorEtiquetasAsync(int[] etiquetaIds) =>
            etiquetaIds.Length == 0
                ? Task.FromResult(new List<EventoTarjeta>())
                : _bd.Eventos.FiltrarAsync(new FiltroEventos { EtiquetaIds = etiquetaIds, Limite = PorFila });

        /// <summary>Lugares con alguna de esas etiquetas, mejor calificados primero.</summary>
        private static List<LugarTarjeta> Lugares(List<LugarTarjeta> lugares, IEnumerable<string> iconos) =>
            lugares.Where(l => l.Etiquetas.Any(t => iconos.Contains(t.Icono)))
                .OrderByDescending(l => l.Calificacion ?? 0).ThenBy(l => l.Nombre)
                .Take(PorFila).ToList();

        /// <summary>
        /// "Carrusel destacado" de Figma: hasta 4 eventos (primero los que destacaron los moderadores,
        /// luego los más guardados) y un lugar al final. Si falta algo, se completa con lo que haya.
        /// </summary>
        private static List<ElementoCollage> ArmarCarrusel(List<EventoTarjeta> eventos, List<LugarTarjeta> lugares)
        {
            const int Total = 5;
            var carrusel = eventos.Take(Total - 1).Select(ElementoCollage.DeEvento).ToList();
            carrusel.AddRange(lugares.Take(Total - carrusel.Count).Select(ElementoCollage.DeLugar));
            return carrusel;
        }

        /// <summary>"Recomendados para ti": dos eventos que más se guardan y dos lugares, como en Figma.</summary>
        private static List<ElementoCollage> ArmarRecomendados(List<EventoTarjeta> eventos, List<LugarTarjeta> lugares)
        {
            var recomendados = eventos.Take(2).Select(ElementoCollage.DeEvento).ToList();
            recomendados.AddRange(lugares.Take(2).Select(ElementoCollage.DeLugar));
            return recomendados;
        }
    }

    /// <summary>Respuesta de "home.obtener". La forma está documentada en wwwroot/js/contenido/datos-demo.js.</summary>
    /// <param name="UsuarioId">Para quién se armó (null = visitante). El Home vuelve a pedir si cambia la sesión.</param>
    public sealed record DatosHome(
        int? UsuarioId,
        IReadOnlyList<ElementoCollage> Carrusel,
        IReadOnlyList<ElementoCollage> Recomendados,
        IReadOnlyList<Etiqueta> Categorias,
        IReadOnlyList<EventoTarjeta> TePodrianInteresar,
        IReadOnlyList<EventoTarjeta> Musica,
        IReadOnlyList<EventoTarjeta> Actividades,
        IReadOnlyList<LugarTarjeta> Cafes,
        IReadOnlyList<LugarTarjeta> ParaLaTarde,
        IReadOnlySet<int> FavoritosEventos,
        IReadOnlySet<int> FavoritosLugares);
}
