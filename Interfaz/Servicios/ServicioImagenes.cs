using System.Net.Http.Headers;
using System.Text.Json;
using BackendLogica.Configuracion;
using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Falso;

namespace Interfaz.Servicios
{
    /// <summary>
    /// Sube las fotos de "Publicar evento" a Cloudinary y devuelve su <c>secure_url</c>, que es lo que
    /// guarda la BD (<c>event.main_image_url</c> y <c>event_image.url</c>).
    /// </summary>
    /// <remarks>
    /// Usa un <b>upload preset unsigned</b>: en <c>dbsettings.local.json</c> van las llaves
    /// <c>"CloudinaryCloudName"</c> y <c>"CloudinaryUploadPreset"</c> (ver dbsettings.example.json).
    /// Con los datos de prueba y sin esas llaves no se sube nada: se usa una foto de ejemplo de Cloudinary
    /// para poder probar la pantalla sin cuenta.
    /// </remarks>
    public sealed class ServicioImagenes(ISuraChihBD bd)
    {
        /// <summary>"Hasta 8 imágenes · 5 MB c/u" del diseño.</summary>
        public const int MaximoFotos = 8;
        public const long MaximoBytes = 5 * 1024 * 1024;
        /// <summary>"JPG o PNG".</summary>
        public const string Formatos = "image/jpeg,image/png";

        private const string FotoDemo = "https://res.cloudinary.com/demo/image/upload/sample.jpg";
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };
        private static int _demo;

        private readonly ConfiguracionBD _config = ConfiguracionBD.Predeterminada;

        private bool Configurado => !string.IsNullOrWhiteSpace(_config.CloudinaryNube) && !string.IsNullOrWhiteSpace(_config.CloudinaryPreset);

        /// <summary>Sin cuenta de Cloudinary y con datos de prueba: las fotos son de ejemplo.</summary>
        public bool ModoPrueba => !Configurado && bd is SuraChihFalso;

        /// <summary>Revisa tipo y tamaño antes de leer el archivo (los mismos límites que dice la pantalla).</summary>
        public static void Validar(string tipo, long bytes)
        {
            if (tipo is not ("image/jpeg" or "image/png"))
                throw new DatosInvalidosException("Solo se pueden subir fotos JPG o PNG.");
            if (bytes > MaximoBytes)
                throw new DatosInvalidosException("Cada foto puede pesar hasta 5 MB.");
        }

        /// <returns>La <c>secure_url</c> (https) de la foto subida.</returns>
        public async Task<string> SubirAsync(Stream contenido, string nombre, string tipo, CancellationToken ct = default)
        {
            if (!Configurado)
            {
                if (ModoPrueba)
                    // La galería no repite URLs: el número las distingue (Cloudinary ignora la query).
                    return $"{FotoDemo}?foto={Interlocked.Increment(ref _demo)}";
                throw new ReglaNegocioException(
                    "Para subir fotos falta configurar Cloudinary (CloudinaryCloudName y CloudinaryUploadPreset en dbsettings.local.json).");
            }

            using var formulario = new MultipartFormDataContent();
            var archivo = new StreamContent(contenido);
            archivo.Headers.ContentType = new MediaTypeHeaderValue(tipo);
            formulario.Add(archivo, "file", nombre);
            formulario.Add(new StringContent(_config.CloudinaryPreset!), "upload_preset");

            HttpResponseMessage respuesta;
            try
            {
                respuesta = await Http.PostAsync($"https://api.cloudinary.com/v1_1/{_config.CloudinaryNube}/image/upload", formulario, ct);
            }
            catch (HttpRequestException)
            {
                throw new ReglaNegocioException("No se pudo subir la foto. Revisa tu conexión a internet.");
            }

            using (respuesta)
            {
                string cuerpo = await respuesta.Content.ReadAsStringAsync(ct);
                if (!respuesta.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[Cloudinary] {(int)respuesta.StatusCode}: {cuerpo}");
                    throw new ReglaNegocioException("Cloudinary no aceptó la foto. Intenta con otra.");
                }
                using JsonDocument json = JsonDocument.Parse(cuerpo);
                return json.RootElement.TryGetProperty("secure_url", out JsonElement url) && url.GetString() is { } segura
                    ? segura
                    : throw new ReglaNegocioException("Cloudinary no devolvió la dirección de la foto.");
            }
        }
    }
}
