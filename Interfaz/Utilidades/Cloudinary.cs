using System.Text.RegularExpressions;

namespace Interfaz.Utilidades
{
    /// <summary>
    /// Optimización de imágenes de Cloudinary.
    /// </summary>
    /// <remarks>
    /// Las fotos de eventos y lugares se guardan en la BD como URL de Cloudinary
    /// (event.main_image_url, place.main_image_url, *_image.url). En lugar de descargar el
    /// original (varios MB), se pide a Cloudinary una versión del tamaño exacto en que se muestra:
    /// <code>
    /// https://res.cloudinary.com/&lt;cuenta&gt;/image/upload/v123/foto.jpg
    /// → https://res.cloudinary.com/&lt;cuenta&gt;/image/upload/f_auto,q_auto,c_fill,g_auto,w_600,h_660/v123/foto.jpg
    /// </code>
    /// Las URL que no son de Cloudinary (p. ej. las imágenes demo locales) se devuelven sin cambios.
    /// </remarks>
    public static partial class Cloudinary
    {
        private const string MarcaUpload = "/image/upload/";
        /// <summary>
        /// Se pide al doble del tamaño CSS para que se vea nítida en pantallas con escala (125 %, 150 %, 200 %).
        /// C# no conoce la densidad de la pantalla, así que se usa el máximo que pedía la versión en JavaScript.
        /// </summary>
        private const int Densidad = 2;

        [GeneratedRegex(@"^(?:(?:w|h|c|f|q|g|e|t|a|b|o|x|y|ar|dpr|fl|bo)_[^/,]+,?)+/")]
        private static partial Regex YaTransformada();

        public static bool EsCloudinary(string? url) =>
            url != null && url.Contains("res.cloudinary.com") && url.Contains(MarcaUpload);

        /// <summary>URL optimizada para mostrarse en una caja de <paramref name="ancho"/> × <paramref name="alto"/> píxeles CSS.</summary>
        public static string UrlOptimizada(string url, int? ancho = null, int? alto = null)
        {
            if (!EsCloudinary(url)) return url;

            var transformaciones = new List<string> { "f_auto", "q_auto" };
            if (ancho != null || alto != null) transformaciones.AddRange(["c_fill", "g_auto"]);
            if (ancho != null) transformaciones.Add($"w_{ancho * Densidad}");
            if (alto != null) transformaciones.Add($"h_{alto * Densidad}");

            int corte = url.IndexOf(MarcaUpload, StringComparison.Ordinal);
            string antes = url[..corte];
            string despues = url[(corte + MarcaUpload.Length)..];
            // Si la URL ya trae transformaciones (p. ej. "w_400,c_fill/v123/..."), se respetan.
            return YaTransformada().IsMatch(despues)
                ? url
                : $"{antes}{MarcaUpload}{string.Join(',', transformaciones)}/{despues}";
        }
    }
}
