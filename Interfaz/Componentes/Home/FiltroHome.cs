using BackendLogica.Modelos;

namespace Interfaz.Componentes.Home
{
    public enum TipoFiltro { Todo, Eventos, Lugares }

    public enum CuandoFiltro { Hoy, FinDeSemana }

    /// <summary>
    /// Filtros rápidos del Home. Se aplican sobre lo que ya llegó de <c>ServicioHome</c>, sin volver a consultar.
    /// </summary>
    public sealed record FiltroHome(TipoFiltro Tipo, CuandoFiltro? Cuando, bool Gratis)
    {
        public static readonly FiltroHome Ninguno = new(TipoFiltro.Todo, null, false);

        /// <summary>"Todo / Eventos / Lugares": ¿se ve este tipo de tarjeta?</summary>
        public bool MuestraEventos => Tipo != TipoFiltro.Lugares;
        public bool MuestraLugares => Tipo != TipoFiltro.Eventos;

        public bool Pasa(EventoTarjeta e, DateTime ahora)
        {
            if (Gratis && (e.PrecioPorConfirmar || e.PrecioMax != 0)) return false;
            return RangoCuando(ahora) is not { } rango || EnRango(e, rango);
        }

        /// <summary>"Hoy" y "Este fin de semana" son de eventos: los lugares no se filtran por fecha.</summary>
        public bool Pasa(LugarTarjeta l) => !Gratis || l.PrecioMax == 0;

        /// <summary>[desde, hasta) del filtro "cuándo", igual que FiltroEventos.RangoFechas de BackendLogica.</summary>
        private (DateTime Desde, DateTime Hasta)? RangoCuando(DateTime ahora)
        {
            DateTime hoy = ahora.Date;
            return Cuando switch
            {
                CuandoFiltro.Hoy => (hoy, hoy.AddDays(1)),
                CuandoFiltro.FinDeSemana => hoy.DayOfWeek switch
                {
                    DayOfWeek.Saturday => (hoy, hoy.AddDays(2)),
                    DayOfWeek.Sunday => (hoy, hoy.AddDays(1)),
                    var dia => (hoy.AddDays(6 - (int)dia), hoy.AddDays(8 - (int)dia)),
                },
                _ => null
            };
        }

        /// <summary>Un evento entra si se cruza con el rango (los de varios días cuentan aunque hayan empezado antes).</summary>
        private static bool EnRango(EventoTarjeta e, (DateTime Desde, DateTime Hasta) rango) =>
            !e.FechaPorConfirmar && e.Inicio < rango.Hasta && e.Fin >= rango.Desde;
    }
}
