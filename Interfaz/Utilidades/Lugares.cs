using BackendLogica.Modelos;

namespace Interfaz.Utilidades
{
    /// <summary>
    /// Tipo de un lugar (café, museo, parque...) y su paso a puntos del mapa (Componentes/Mapa/Mapa.razor).
    /// </summary>
    /// <remarks>
    /// El tipo sale de las etiquetas del lugar: la primera cuyo <c>Icono</c> (tag.icon en la BD) esté en
    /// <see cref="Tipos"/>. Los que no están (restaurant, bar, stage...) usan el pin genérico y no tienen
    /// píldora en el mapa; para agregar uno, exporta su icono de Figma a assets/iconos y agrégalo aquí.
    /// </remarks>
    public static class Lugares
    {
        /// <summary>Por tag.icon: texto de la píldora del mapa e icono del pin (assets/iconos). En el orden de Figma.</summary>
        private static readonly (string Icono, string Plural, string Pin)[] Tipos =
        [
            ("coffee", "Cafés", "pin-cafe"),
            ("museum", "Museos", "pin-museo"),
            ("tree", "Parques", "pin-parque"),
        ];
        private const string PinGenerico = "ubicacion";

        /// <summary>Etiqueta que define el tipo del lugar o, si ninguna es de un tipo conocido, la primera.</summary>
        private static Etiqueta? EtiquetaPrincipal(LugarTarjeta lugar) =>
            lugar.Etiquetas.FirstOrDefault(t => Tipos.Any(x => x.Icono == t.Icono)) ?? lugar.Etiquetas.FirstOrDefault();

        /// <summary>Clave del tipo ('coffee', 'museum', 'tree') o null si no es de un tipo conocido.</summary>
        public static string? TipoLugar(LugarTarjeta lugar)
        {
            string? icono = EtiquetaPrincipal(lugar)?.Icono;
            return Tipos.Any(t => t.Icono == icono) ? icono : null;
        }

        /// <summary>"Café, C. Tercera 805, Zona Centro": tipo y dirección para la segunda línea de una fila.</summary>
        public static string Subtitulo(LugarTarjeta lugar) =>
            string.Join(", ", new[] { EtiquetaPrincipal(lugar)?.Nombre, lugar.Direccion }.Where(t => !string.IsNullOrEmpty(t)));

        /// <summary>Píldoras de tipo que tiene sentido mostrar: solo los tipos que hay en la lista.</summary>
        public static List<(string Valor, string Texto)> TiposPresentes(IEnumerable<LugarTarjeta> lugares)
        {
            var presentes = lugares.Select(TipoLugar).ToHashSet();
            return Tipos.Where(t => presentes.Contains(t.Icono)).Select(t => (t.Icono, t.Plural)).ToList();
        }

        /// <summary>Punto del mapa para un lugar.</summary>
        public static PuntoMapa Punto(LugarTarjeta lugar)
        {
            string? tipo = TipoLugar(lugar);
            string pin = Tipos.FirstOrDefault(t => t.Icono == tipo).Pin ?? PinGenerico;
            return new PuntoMapa(lugar.Id, "lugar", lugar.Latitud, lugar.Longitud, lugar.Nombre, pin);
        }
    }

    /// <summary>
    /// Un pin del mapa. <paramref name="Tipo"/> distingue un evento de un lugar con el mismo id;
    /// <paramref name="Icono"/> es un archivo de assets/iconos sin extensión.
    /// </summary>
    public sealed record PuntoMapa(int Id, string Tipo, decimal? Lat, decimal? Lng, string? Titulo, string Icono);
}
