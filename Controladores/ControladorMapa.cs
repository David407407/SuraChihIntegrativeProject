using BackendLogica.Contratos;
using BackendLogica.Modelos;
using SuraChihIntegrativeProject.Puente;

namespace SuraChihIntegrativeProject.Controladores
{
    /// <summary>
    /// Acciones de la pantalla Mapa (wwwroot/js/mapa.js). El mapa en sí es el componente
    /// <c>&lt;sc-mapa&gt;</c> (Leaflet); aquí solo se mandan los datos como JSON por el puente.
    /// </summary>
    public sealed class ControladorMapa
    {
        private readonly ISuraChihBD _bd;

        public ControladorMapa(ISuraChihBD bd)
        {
            _bd = bd;
        }

        public void Registrar(PuenteWeb puente)
        {
            puente.Registrar("mapa.obtener", async () => await ObtenerMapaAsync());
        }

        /// <summary>
        /// Todos los lugares aprobados con sus coordenadas. Son pocos, así que el buscador y las
        /// píldoras de tipo (Cafés, Museos, Parques) filtran en el JavaScript sin volver a consultar.
        /// </summary>
        private async Task<DatosMapa> ObtenerMapaAsync() =>
            new(await _bd.Lugares.ListarAprobadosAsync());
    }

    /// <summary>Respuesta de "mapa.obtener". La forma está documentada en wwwroot/js/contenido/datos-demo.js.</summary>
    public sealed record DatosMapa(IReadOnlyList<LugarTarjeta> Lugares);
}
