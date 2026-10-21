using BackendLogica.Contratos;
using BackendLogica.Modelos;

namespace Interfaz.Servicios
{
    /// <summary>
    /// Datos de la landing (Paginas/Landing.razor). Aquí solo se decide QUÉ datos pedir;
    /// las consultas viven en BackendLogica y el formato de textos en Utilidades/Formato.cs.
    /// </summary>
    public sealed class ServicioLanding(ISuraChihBD bd)
    {
        /// <summary>Todo lo dinámico de la landing en una sola llamada (las consultas corren en paralelo).</summary>
        public async Task<DatosLanding> ObtenerAsync()
        {
            var resumen = bd.ObtenerResumenAsync();
            var destacados = bd.Eventos.ListarDestacadosAsync(limite: 3);
            var masGuardados = bd.Eventos.ListarMasGuardadosAsync(limite: 5);
            var lugares = bd.Lugares.ListarRecomendadosAsync(limite: 4);
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
    }

    public sealed record DatosLanding(
        ResumenPlataforma Resumen,
        IReadOnlyList<ElementoCollage> Collage,
        IReadOnlyList<EventoTarjeta> Eventos,
        IReadOnlyList<LugarTarjeta> Lugares);

    /// <summary>Tarjeta del collage o del carrusel: un evento o un lugar (la otra propiedad va en null).</summary>
    public sealed record ElementoCollage(EventoTarjeta? Evento = null, LugarTarjeta? Lugar = null)
    {
        public bool EsLugar => Lugar != null;

        public static ElementoCollage DeEvento(EventoTarjeta e) => new(Evento: e);
        public static ElementoCollage DeLugar(LugarTarjeta l) => new(Lugar: l);
    }
}
