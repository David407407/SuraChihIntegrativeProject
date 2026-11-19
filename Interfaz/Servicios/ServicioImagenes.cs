using System.Net.Http.Headers;
using System.Text.Json;
using BackendLogica.Configuracion;
using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Falso;
using Microsoft.AspNetCore.Components.Forms;

namespace Interfaz.Servicios
{
    /// <summary>
    /// Fotos de los formularios de publicación ("Publicar evento" y, cuando exista, "Registrar lugar").
    /// Al elegirlas solo se leen en memoria (<see cref="PrepararAsync"/>); se suben a Cloudinary hasta que
    /// el organizador guarda (<see cref="SubirPendientesAsync"/>), y su <c>secure_url</c> es lo que guarda la BD
    /// (<c>event.main_image_url</c> y <c>event_image.url</c>). Así no quedan fotos sueltas de eventos que
    /// nunca se publicaron.
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

        /// <summary>Videos del local en "Hazte organizador": hasta 3, de 50 MB c/u (Cloudinary gratis acepta hasta 100 MB).</summary>
        public const int MaximoVideos = BackendLogica.Seguridad.Validar.MaximoVideos;
        public const long MaximoBytesVideo = 50 * 1024 * 1024;
        /// <summary>"MP4, MOV o WEBM".</summary>
        public const string FormatosVideo = "video/mp4,video/quicktime,video/webm";

        private const string FotoDemo = "https://res.cloudinary.com/demo/image/upload/sample.jpg";
        private const string VideoDemo = "https://res.cloudinary.com/demo/video/upload/dog.mp4";
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };
        /// <summary>Los videos pesan más: se les da más tiempo para subir.</summary>
        private static readonly HttpClient HttpVideos = new() { Timeout = TimeSpan.FromMinutes(5) };
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

        /// <summary>Tamaño de la miniatura que se muestra mientras la foto no se sube (la portada mide 320×180).</summary>
        private const int AnchoVista = 640, AltoVista = 360;

        /// <summary>
        /// Lee la foto elegida (o arrastrada) sin subirla: guarda el archivo en memoria y arma una miniatura
        /// (la reduce el navegador) para verla en el formulario.
        /// </summary>
        public static async Task<FotoFormulario> PrepararAsync(IBrowserFile archivo, CancellationToken ct = default)
        {
            Validar(archivo.ContentType, archivo.Size);

            using var original = new MemoryStream();
            await using (Stream entrada = archivo.OpenReadStream(MaximoBytes, ct))
                await entrada.CopyToAsync(original, ct);

            IBrowserFile chica = await archivo.RequestImageFileAsync("image/jpeg", AnchoVista, AltoVista);
            using var vista = new MemoryStream();
            await using (Stream entrada = chica.OpenReadStream(MaximoBytes, ct))
                await entrada.CopyToAsync(vista, ct);

            return FotoFormulario.Nueva(original.ToArray(), archivo.Name, archivo.ContentType,
                $"data:image/jpeg;base64,{Convert.ToBase64String(vista.ToArray())}");
        }

        /// <summary>
        /// Sube a Cloudinary, en orden, las fotos que todavía no tienen URL. Se llama al guardar o publicar.
        /// Si una falla, las que ya subieron conservan su URL y el error dice cuál fue.
        /// </summary>
        public async Task SubirPendientesAsync(IEnumerable<FotoFormulario> fotos, CancellationToken ct = default)
        {
            foreach (FotoFormulario foto in fotos.Where(f => f.Pendiente).ToList())
            {
                try
                {
                    using var contenido = new MemoryStream(foto.Contenido!, writable: false);
                    foto.MarcarSubida(await SubirAsync(contenido, foto.Nombre!, foto.Tipo!, ct));
                }
                catch (SuraChihException ex)
                {
                    throw new ReglaNegocioException($"No se pudo subir la foto {foto.Nombre}. {ex.Message}", ex);
                }
            }
        }

        // ---------------------------------------------------------------- Videos ("Hazte organizador")

        /// <summary>Revisa tipo y tamaño de un video antes de leerlo.</summary>
        public static void ValidarVideo(string tipo, long bytes)
        {
            if (tipo is not ("video/mp4" or "video/quicktime" or "video/webm"))
                throw new DatosInvalidosException("Solo se pueden subir videos MP4, MOV o WEBM.");
            if (bytes > MaximoBytesVideo)
                throw new DatosInvalidosException("Cada video puede pesar hasta 50 MB.");
        }

        /// <summary>Lee el video elegido en memoria, sin subirlo (se sube al enviar la solicitud).</summary>
        public static async Task<VideoFormulario> PrepararVideoAsync(IBrowserFile archivo, CancellationToken ct = default)
        {
            ValidarVideo(archivo.ContentType, archivo.Size);
            using var memoria = new MemoryStream();
            await using (Stream entrada = archivo.OpenReadStream(MaximoBytesVideo, ct))
                await entrada.CopyToAsync(memoria, ct);
            return VideoFormulario.Nuevo(memoria.ToArray(), archivo.Name, archivo.ContentType);
        }

        /// <summary>Sube a Cloudinary, en orden, los videos que todavía no tienen URL.</summary>
        public async Task SubirVideosPendientesAsync(IEnumerable<VideoFormulario> videos, CancellationToken ct = default)
        {
            foreach (VideoFormulario video in videos.Where(v => v.Pendiente).ToList())
            {
                try
                {
                    using var contenido = new MemoryStream(video.Contenido!, writable: false);
                    video.MarcarSubido(await SubirAsync(contenido, video.Nombre, video.Tipo!, ct, esVideo: true));
                }
                catch (SuraChihException ex)
                {
                    throw new ReglaNegocioException($"No se pudo subir el video {video.Nombre}. {ex.Message}", ex);
                }
            }
        }

        /// <returns>La <c>secure_url</c> (https) de la foto o el video subido.</returns>
        private async Task<string> SubirAsync(Stream contenido, string nombre, string tipo, CancellationToken ct = default, bool esVideo = false)
        {
            string que = esVideo ? "videos" : "fotos";
            if (!Configurado)
            {
                if (ModoPrueba)
                    // La galería no repite URLs: el número las distingue (Cloudinary ignora la query).
                    return $"{(esVideo ? VideoDemo : FotoDemo)}?{(esVideo ? "video" : "foto")}={Interlocked.Increment(ref _demo)}";
                throw new ReglaNegocioException(
                    $"Para subir {que} falta configurar Cloudinary (CloudinaryCloudName y CloudinaryUploadPreset en dbsettings.local.json).");
            }

            using var formulario = new MultipartFormDataContent();
            var archivo = new StreamContent(contenido);
            archivo.Headers.ContentType = new MediaTypeHeaderValue(tipo);
            formulario.Add(archivo, "file", nombre);
            formulario.Add(new StringContent(_config.CloudinaryPreset!), "upload_preset");

            HttpResponseMessage respuesta;
            try
            {
                // Cloudinary separa fotos (image/upload) y videos (video/upload); el mismo preset sirve si permite videos.
                HttpClient cliente = esVideo ? HttpVideos : Http;
                respuesta = await cliente.PostAsync($"https://api.cloudinary.com/v1_1/{_config.CloudinaryNube}/{(esVideo ? "video" : "image")}/upload", formulario, ct);
            }
            catch (HttpRequestException)
            {
                throw new ReglaNegocioException($"No se pudo subir {(esVideo ? "el video" : "la foto")}. Revisa tu conexión a internet.");
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new ReglaNegocioException($"Se tardó demasiado en subir {(esVideo ? "el video" : "la foto")}. Revisa tu conexión a internet.");
            }

            using (respuesta)
            {
                string cuerpo = await respuesta.Content.ReadAsStringAsync(ct);
                if (!respuesta.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[Cloudinary] {(int)respuesta.StatusCode}: {cuerpo}");
                    throw new ReglaNegocioException(esVideo
                        ? "Cloudinary no aceptó el video. Revisa que tu upload preset permita videos, o intenta con otro."
                        : "Cloudinary no aceptó la foto. Intenta con otra.");
                }
                using JsonDocument json = JsonDocument.Parse(cuerpo);
                return json.RootElement.TryGetProperty("secure_url", out JsonElement url) && url.GetString() is { } segura
                    ? segura
                    : throw new ReglaNegocioException($"Cloudinary no devolvió la dirección {(esVideo ? "del video" : "de la foto")}.");
            }
        }
    }

    /// <summary>
    /// Una foto del formulario: ya guardada (<see cref="Url"/> de Cloudinary) o elegida pero aún sin subir
    /// (<see cref="Pendiente"/>, con el archivo en memoria y una miniatura en <see cref="Vista"/>).
    /// </summary>
    public sealed class FotoFormulario
    {
        private FotoFormulario() { }

        /// <summary><c>secure_url</c> de Cloudinary; null mientras no se sube.</summary>
        public string? Url { get; private set; }
        /// <summary>Lo que se pinta en el formulario: la URL o la miniatura (data:image/jpeg).</summary>
        public string Vista { get; private set; } = "";
        public byte[]? Contenido { get; private set; }
        public string? Nombre { get; private init; }
        public string? Tipo { get; private init; }

        public bool Pendiente => Url == null;

        /// <summary>Foto que ya estaba guardada en la BD (al editar).</summary>
        public static FotoFormulario Existente(string url) => new() { Url = url, Vista = url };

        public static FotoFormulario Nueva(byte[] contenido, string nombre, string tipo, string vista) =>
            new() { Contenido = contenido, Nombre = nombre, Tipo = tipo, Vista = vista };

        internal void MarcarSubida(string url)
        {
            Url = url;
            Vista = url;
            Contenido = null;   // ya no hace falta tenerla en memoria
        }
    }

    /// <summary>
    /// Un video del local en "Hazte organizador": ya subido (<see cref="Url"/> de Cloudinary) o elegido pero aún
    /// sin subir (<see cref="Pendiente"/>, con el archivo en memoria).
    /// </summary>
    public sealed class VideoFormulario
    {
        private VideoFormulario() { }

        /// <summary><c>secure_url</c> de Cloudinary; null mientras no se sube.</summary>
        public string? Url { get; private set; }
        public byte[]? Contenido { get; private set; }
        /// <summary>Nombre del archivo (o "Video 1" si ya estaba guardado).</summary>
        public string Nombre { get; private init; } = "";
        public string? Tipo { get; private init; }
        /// <summary>Tamaño en bytes (0 si ya estaba guardado).</summary>
        public long Bytes { get; private init; }

        public bool Pendiente => Url == null;

        /// <summary>Video que ya estaba guardado (al reenviar una solicitud rechazada).</summary>
        public static VideoFormulario Existente(string url, int numero) => new() { Url = url, Nombre = $"Video {numero}" };

        public static VideoFormulario Nuevo(byte[] contenido, string nombre, string tipo) =>
            new() { Contenido = contenido, Nombre = nombre, Tipo = tipo, Bytes = contenido.LongLength };

        internal void MarcarSubido(string url)
        {
            Url = url;
            Contenido = null;   // ya no hace falta tenerlo en memoria
        }
    }
}
