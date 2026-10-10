using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using BackendLogica.Datos;
using Microsoft.Web.WebView2.Core;

namespace SuraChihIntegrativeProject.Puente
{
    /// <summary>
    /// Canal de mensajes entre la interfaz web (wwwroot/js/nucleo/puente.js) y C#.
    /// </summary>
    /// <remarks>
    /// <para>La página manda <c>{ id, accion, datos }</c>. Se busca el manejador registrado para
    /// <c>accion</c>, se ejecuta y se responde <c>{ id, ok: true, datos }</c> o
    /// <c>{ id, ok: false, error: { codigo, mensaje } }</c>.</para>
    /// <para>Los datos viajan como JSON en camelCase; los enums como texto ("aprobado").</para>
    /// </remarks>
    public sealed class PuenteWeb
    {
        /// <summary>Opciones JSON compartidas (las mismas que espera el JavaScript).</summary>
        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly CoreWebView2 _web;
        private readonly Dictionary<string, Func<JsonElement?, Task<object?>>> _acciones = new(StringComparer.Ordinal);

        public PuenteWeb(CoreWebView2 web)
        {
            _web = web;
            _web.WebMessageReceived += AlRecibirMensaje;
        }

        /// <summary>Registra una acción sin datos de entrada.</summary>
        public void Registrar(string accion, Func<Task<object?>> manejador) =>
            _acciones[accion] = _ => manejador();

        /// <summary>Registra una acción cuyos datos se convierten a <typeparamref name="TDatos"/>.</summary>
        public void Registrar<TDatos>(string accion, Func<TDatos, Task<object?>> manejador) =>
            _acciones[accion] = async json =>
            {
                TDatos? datos = json is { ValueKind: not JsonValueKind.Null } valor ? valor.Deserialize<TDatos>(Json) : default;
                if (datos == null) throw new SolicitudInvalidaException($"La acción '{accion}' necesita datos.");
                return await manejador(datos);
            };

        // El evento llega en el hilo de la interfaz; al hacer await se regresa a él,
        // que es donde CoreWebView2 permite responder.
        private async void AlRecibirMensaje(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            MensajeEntrante? mensaje;
            try
            {
                mensaje = JsonSerializer.Deserialize<MensajeEntrante>(e.WebMessageAsJson, Json);
            }
            catch (JsonException)
            {
                return;   // no es un mensaje del puente
            }
            if (mensaje == null) return;

            try
            {
                if (!_acciones.TryGetValue(mensaje.Accion, out var manejador))
                    throw new SolicitudInvalidaException($"Acción desconocida: {mensaje.Accion}");

                object? resultado = await manejador(mensaje.Datos);
                Responder(new { id = mensaje.Id, ok = true, datos = resultado });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Puente] {mensaje.Accion}: {ex}");
                Responder(new { id = mensaje.Id, ok = false, error = ErrorParaWeb(ex) });
            }
        }

        private void Responder(object respuesta) =>
            _web.PostWebMessageAsJson(JsonSerializer.Serialize(respuesta, Json));

        /// <summary>Código estable para que el JavaScript decida qué hacer + mensaje para el usuario.</summary>
        private static object ErrorParaWeb(Exception ex) => ex switch
        {
            RequiereSesionException => new { codigo = "requiere-sesion", mensaje = ex.Message },
            SolicitudInvalidaException => new { codigo = "solicitud-invalida", mensaje = ex.Message },
            SinConexionException => new { codigo = "sin-conexion", mensaje = ex.Message },
            ReglaNegocioException => new { codigo = "regla-negocio", mensaje = ex.Message },
            DatosInvalidosException => new { codigo = "datos-invalidos", mensaje = ex.Message },
            DuplicadoException => new { codigo = "duplicado", mensaje = ex.Message },
            SuraChihException => new { codigo = "interno", mensaje = ex.Message },
            _ => new { codigo = "interno", mensaje = "Algo salió mal. Intenta de nuevo." }
        };

        private sealed record MensajeEntrante(int Id, string Accion, JsonElement? Datos);
    }

    /// <summary>La acción necesita un usuario con sesión iniciada.</summary>
    public sealed class RequiereSesionException(string mensaje) : Exception(mensaje);

    /// <summary>La página mandó una acción o datos que C# no entiende (error de programación).</summary>
    public sealed class SolicitudInvalidaException(string mensaje) : Exception(mensaje);
}
