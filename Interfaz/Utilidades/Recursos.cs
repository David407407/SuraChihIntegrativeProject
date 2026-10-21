namespace Interfaz.Utilidades
{
    /// <summary>
    /// Rutas de los archivos de <c>Interfaz/wwwroot</c>. Al ser una biblioteca Razor, sus archivos se
    /// sirven bajo <c>_content/Interfaz/</c> tanto en la app de escritorio como en el navegador.
    /// </summary>
    public static class Recursos
    {
        public const string Base = "_content/Interfaz";

        /// <summary>Icono SVG exportado de Figma (assets/iconos), sin extensión.</summary>
        public static string Icono(string nombre) => $"{Base}/assets/iconos/{nombre}.svg";

        /// <summary>Foto de ejemplo de assets/demo (solo el catálogo de componentes).</summary>
        public static string Demo(string archivo) => $"{Base}/assets/demo/{archivo}";
    }
}
