using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BackendLogica.Configuracion;
using BackendLogica.Datos;

namespace Interfaz.Servicios
{
    /// <summary>
    /// "Continuar con Google" para la app de escritorio (OAuth 2.0 para apps instaladas, con PKCE).
    /// </summary>
    /// <remarks>
    /// Google no deja iniciar sesión dentro de un WebView, así que:
    /// <list type="number">
    /// <item>Se abre una dirección local (http://127.0.0.1:puerto/) que escucha la respuesta.</item>
    /// <item>Se abre el navegador del sistema en la página de Google para elegir la cuenta.</item>
    /// <item>Google regresa a la dirección local con un código; se cambia por un token (con el secreto y el
    /// verificador PKCE) y con él se piden correo, nombre y foto.</item>
    /// </list>
    /// Las llaves salen de <c>dbsettings.local.json</c> (<c>GoogleClientId</c>, <c>GoogleClientSecret</c>).
    /// Como el token llega directo de Google por HTTPS a cambio de nuestro código, no hace falta verificar
    /// la firma del id_token.
    /// </remarks>
    public sealed class ServicioGoogle
    {
        private const string Autorizar = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string Token = "https://oauth2.googleapis.com/token";
        private const string DatosUsuario = "https://openidconnect.googleapis.com/v1/userinfo";
        /// <summary>Tiempo para elegir la cuenta en el navegador.</summary>
        private static readonly TimeSpan Espera = TimeSpan.FromMinutes(5);
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

        private readonly ConfiguracionBD _config = ConfiguracionBD.Predeterminada;

        public bool Configurado =>
            !string.IsNullOrWhiteSpace(_config.GoogleClientId) && !string.IsNullOrWhiteSpace(_config.GoogleClientSecret);

        /// <summary>Abre el navegador, espera a que el usuario elija su cuenta y devuelve sus datos.</summary>
        /// <exception cref="ReglaNegocioException">Faltan las llaves, el usuario canceló o Google no respondió.</exception>
        public async Task<PerfilGoogle> IniciarAsync(CancellationToken ct = default)
        {
            if (!Configurado)
                throw new ReglaNegocioException("Falta configurar Google (GoogleClientId y GoogleClientSecret en dbsettings.local.json).");

            string redireccion = $"http://127.0.0.1:{PuertoLibre()}/";
            string estado = Aleatorio(16);
            string verificador = Aleatorio(48);
            string reto = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verificador)));

            using var escucha = new HttpListener();
            escucha.Prefixes.Add(redireccion);
            escucha.Start();

            string url = $"{Autorizar}?response_type=code&scope={Uri.EscapeDataString("openid email profile")}" +
                         $"&client_id={Uri.EscapeDataString(_config.GoogleClientId!)}" +
                         $"&redirect_uri={Uri.EscapeDataString(redireccion)}" +
                         $"&state={estado}&code_challenge={reto}&code_challenge_method=S256&prompt=select_account";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

            string codigo = await EsperarCodigoAsync(escucha, estado, ct);
            string accessToken = await CambiarCodigoAsync(codigo, verificador, redireccion, ct);
            return await PedirPerfilAsync(accessToken, ct);
        }

        /// <summary>Espera la vuelta del navegador y le contesta con una página para regresar a la app.</summary>
        private static async Task<string> EsperarCodigoAsync(HttpListener escucha, string estado, CancellationToken ct)
        {
            var contexto = escucha.GetContextAsync();
            if (await Task.WhenAny(contexto, Task.Delay(Espera, ct)) != contexto)
                throw new ReglaNegocioException("Se acabó el tiempo para elegir tu cuenta de Google. Intenta de nuevo.");

            HttpListenerContext c = await contexto;
            var datos = System.Web.HttpUtility.ParseQueryString(c.Request.Url?.Query ?? "");
            string? error = datos["error"], codigo = datos["code"];
            bool valido = error == null && codigo != null && datos["state"] == estado;

            await ResponderAsync(c.Response, valido
                ? "Listo, ya puedes volver a SuraChih."
                : "No se completó el inicio de sesión. Vuelve a SuraChih e inténtalo de nuevo.");

            if (error == "access_denied") throw new ReglaNegocioException("Cancelaste el inicio de sesión con Google.");
            if (!valido) throw new ReglaNegocioException("Google no completó el inicio de sesión. Intenta de nuevo.");
            return codigo!;
        }

        private static async Task ResponderAsync(HttpListenerResponse respuesta, string mensaje)
        {
            string html = $$"""
                <!doctype html><html lang="es"><head><meta charset="utf-8"><title>SuraChih</title>
                <style>body{font-family:system-ui,sans-serif;display:grid;place-items:center;height:100vh;margin:0;background:#f3f6fb;color:#0b1b3a}
                p{font-size:20px;font-weight:600;background:#fff;padding:32px 40px;border-radius:24px}</style></head>
                <body><p>{{WebUtility.HtmlEncode(mensaje)}}</p></body></html>
                """;
            byte[] bytes = Encoding.UTF8.GetBytes(html);
            respuesta.ContentType = "text/html; charset=utf-8";
            respuesta.ContentLength64 = bytes.Length;
            await respuesta.OutputStream.WriteAsync(bytes);
            respuesta.Close();
        }

        private async Task<string> CambiarCodigoAsync(string codigo, string verificador, string redireccion, CancellationToken ct)
        {
            using var cuerpo = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = codigo,
                ["client_id"] = _config.GoogleClientId!,
                ["client_secret"] = _config.GoogleClientSecret!,
                ["redirect_uri"] = redireccion,
                ["grant_type"] = "authorization_code",
                ["code_verifier"] = verificador,
            });
            using var respuesta = await Http.PostAsync(Token, cuerpo, ct);
            string texto = await respuesta.Content.ReadAsStringAsync(ct);
            if (!respuesta.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[Google] token {(int)respuesta.StatusCode}: {texto}");
                throw new ReglaNegocioException("Google no aceptó el inicio de sesión. Revisa las llaves de Google en dbsettings.local.json.");
            }
            using var json = JsonDocument.Parse(texto);
            return json.RootElement.GetProperty("access_token").GetString()!;
        }

        private static async Task<PerfilGoogle> PedirPerfilAsync(string accessToken, CancellationToken ct)
        {
            using var solicitud = new HttpRequestMessage(HttpMethod.Get, DatosUsuario);
            solicitud.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            using var respuesta = await Http.SendAsync(solicitud, ct);
            if (!respuesta.IsSuccessStatusCode)
                throw new ReglaNegocioException("No se pudieron leer los datos de tu cuenta de Google.");

            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
            var r = json.RootElement;
            string? Texto(string nombre) => r.TryGetProperty(nombre, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            bool verificado = r.TryGetProperty("email_verified", out var ev) && ev.ValueKind == JsonValueKind.True;

            return new PerfilGoogle(
                Texto("sub") ?? throw new ReglaNegocioException("Google no devolvió tu cuenta."),
                Texto("email") ?? throw new ReglaNegocioException("Tu cuenta de Google no compartió su correo."),
                verificado,
                Texto("name"),
                Texto("picture"));
        }

        private static int PuertoLibre()
        {
            var socket = new TcpListener(IPAddress.Loopback, 0);
            socket.Start();
            int puerto = ((IPEndPoint)socket.LocalEndpoint).Port;
            socket.Stop();
            return puerto;
        }

        private static string Aleatorio(int bytes) => Base64Url(RandomNumberGenerator.GetBytes(bytes));

        private static string Base64Url(byte[] datos) =>
            Convert.ToBase64String(datos).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <param name="Id">"sub": el id de la cuenta de Google (no cambia aunque cambie el correo).</param>
    /// <param name="CorreoVerificado">Google confirmó que el correo es de esa persona.</param>
    /// <param name="FotoUrl">Foto de perfil de Google (https).</param>
    public sealed record PerfilGoogle(string Id, string Correo, bool CorreoVerificado, string? Nombre, string? FotoUrl);
}
