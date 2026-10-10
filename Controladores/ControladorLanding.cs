using BackendLogica;
using BackendLogica.Modelos;
using SuraChihIntegrativeProject.Puente;
using SuraChihIntegrativeProject.Servicios;

namespace SuraChihIntegrativeProject.Controladores
{
    /// <summary>
    /// Acciones que usa la landing (wwwroot/js/main.js). Aquí solo se decide QUÉ datos pedir;
    /// las consultas viven en BackendLogica y el formato de textos en el JavaScript.
    /// </summary>
    public sealed class ControladorLanding
    {
        private readonly SuraChihBD _bd;
        private readonly Sesion _sesion;

        public ControladorLanding(SuraChihBD bd, Sesion sesion)
        {
            _bd = bd;
            _sesion = sesion;
        }

        public void Registrar(PuenteWeb puente)
        {
            puente.Registrar("landing.obtener", async () => await ObtenerLandingAsync());
            puente.Registrar("sesion.obtener", () => Task.FromResult<object?>(_sesion.Usuario));
            puente.Registrar<SolicitudFavorito>("favoritos.alternar", async s => await AlternarFavoritoAsync(s));
        }

        /// <summary>Todo lo dinámico de la landing en una sola llamada (las consultas corren en paralelo).</summary>
        private async Task<DatosLanding> ObtenerLandingAsync()
        {
            var resumen = _bd.ObtenerResumenAsync();
            var destacados = _bd.Eventos.ListarDestacadosAsync(limite: 3);
            var masGuardados = _bd.Eventos.ListarMasGuardadosAsync(limite: 5);
            var lugares = _bd.Lugares.ListarRecomendadosAsync(limite: 4);
            await Task.WhenAll(resumen, destacados, masGuardados, lugares);

            return new DatosLanding(
                resumen.Result,
                ArmarCollage(destacados.Result, masGuardados.Result, lugares.Result),
                masGuardados.Result,
                lugares.Result);
        }

        /// <summary>
        /// Tres tarjetas para el collage del hero, como en Figma: dos eventos (primero los que los
        /// moderadores destacaron) y un lugar. Si falta algo, se completa con lo que haya.
        /// </summary>
        private static List<ElementoCollage> ArmarCollage(
            List<EventoTarjeta> destacados, List<EventoTarjeta> masGuardados, List<LugarTarjeta> lugares)
        {
            const int Total = 3;
            var eventos = destacados.Concat(masGuardados).DistinctBy(e => e.Id).ToList();

            var collage = eventos.Take(2).Select(ElementoCollage.DeEvento).ToList();
            collage.AddRange(lugares.Take(Total - collage.Count).Select(ElementoCollage.DeLugar));
            collage.AddRange(eventos.Skip(2).Take(Total - collage.Count).Select(ElementoCollage.DeEvento));
            return collage;
        }

        private async Task<bool> AlternarFavoritoAsync(SolicitudFavorito solicitud)
        {
            Usuario usuario = _sesion.Requerir("guardar tus favoritos");
            if (solicitud.Tipo != "evento")
                throw new SolicitudInvalidaException("Por ahora solo se pueden guardar eventos en favoritos.");
            return await _bd.Favoritos.AlternarAsync(usuario.Id, solicitud.Id);
        }

        private sealed record SolicitudFavorito(string Tipo, int Id);
    }

    /// <summary>Respuesta de "landing.obtener". La forma está documentada en wwwroot/js/contenido/datos-demo.js.</summary>
    public sealed record DatosLanding(
        ResumenPlataforma Resumen,
        IReadOnlyList<ElementoCollage> Collage,
        IReadOnlyList<EventoTarjeta> Eventos,
        IReadOnlyList<LugarTarjeta> Lugares);

    /// <summary>Tarjeta del collage: un evento o un lugar (la otra propiedad no se envía).</summary>
    public sealed record ElementoCollage(string Tipo, EventoTarjeta? Evento = null, LugarTarjeta? Lugar = null)
    {
        public static ElementoCollage DeEvento(EventoTarjeta e) => new("evento", Evento: e);
        public static ElementoCollage DeLugar(LugarTarjeta l) => new("lugar", Lugar: l);
    }
}
