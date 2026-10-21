namespace Interfaz.Utilidades
{
    /// <summary>
    /// Estilo de cada categoría de evento según el campo <c>Icono</c> de la etiqueta (tag.icon en la BD).
    /// Las etiquetas que no están aquí (las de lugares: coffee, museum...) se pintan con fondo neutro y sin icono.
    /// </summary>
    public static class Categorias
    {
        private static readonly Dictionary<string, string> Tipos = new()
        {
            ["family"] = "familiar",
            ["music"] = "musica",
            ["run"] = "deportes",
            ["robot"] = "tecnologia",
            ["food"] = "gastronomia",
        };

        /// <summary>"musica", "familiar"... o null si la etiqueta no es de una categoría de evento.</summary>
        public static string? Tipo(string? icono) =>
            icono != null && Tipos.TryGetValue(icono, out string? tipo) ? tipo : null;

        /// <summary>Icono de 16px de la categoría (assets/iconos/cat-*.svg), o null si no tiene uno en Figma.</summary>
        public static string? Icono(string? icono) => Tipo(icono) is { } tipo ? $"cat-{tipo}" : null;
    }
}
