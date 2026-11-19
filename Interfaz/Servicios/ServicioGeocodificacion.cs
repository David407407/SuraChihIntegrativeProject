using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using BackendLogica.Datos;

namespace Interfaz.Servicios
{
    /// <summary>
    /// Convierte una dirección escrita ("Av. Universidad 2500, San Felipe") en coordenadas para poner el pin del
    /// minimapa en "Registrar lugar" y "Publicar evento" (ver Componentes/Mapa/CampoDireccion.razor).
    /// </summary>
    /// <remarks>
    /// Usa Nominatim, el buscador gratuito de OpenStreetMap (los mismos datos del mapa). Sus reglas de uso piden:
    /// máximo 1 búsqueda por segundo, un User-Agent que diga qué app es, y no buscar en cada tecla. Por eso aquí
    /// hay un turno de 1 segundo entre búsquedas, se guardan las respuestas ya buscadas, y el campo espera a que
    /// el usuario deje de escribir. Solo busca dentro del estado de Chihuahua (los mismos límites que la BD).
    /// </remarks>
    public sealed partial class ServicioGeocodificacion
    {
        /// <summary>Caja del estado de Chihuahua: oeste, norte, este, sur (como la pide Nominatim).</summary>
        private const string CajaChihuahua = "-109.2,31.8,-103.3,25.5";
        private const decimal Sur = 25.5m, Norte = 31.8m, Oeste = -109.2m, Este = -103.3m;

        private static readonly HttpClient Http = CrearCliente();
        /// <summary>Una búsqueda a la vez, con al menos 1.1 s entre una y otra (regla de Nominatim).</summary>
        private static readonly SemaphoreSlim Turno = new(1, 1);
        private static DateTime _ultima = DateTime.MinValue;
        /// <summary>Direcciones ya buscadas (también las que no se encontraron, como null).</summary>
        private static readonly ConcurrentDictionary<string, (decimal Lat, decimal Lng)?> Guardadas = new(StringComparer.OrdinalIgnoreCase);

        private static HttpClient CrearCliente()
        {
            var cliente = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            cliente.DefaultRequestHeaders.UserAgent.ParseAdd("SuraChih/1.0 (proyecto integrador UTCH; app de escritorio)");
            cliente.DefaultRequestHeaders.AcceptLanguage.ParseAdd("es-MX,es;q=0.9");
            return cliente;
        }

        /// <summary>Vale la pena buscar: al menos 6 caracteres y alguna letra (no solo "123").</summary>
        public static bool ValeLaPena(string? direccion) =>
            direccion != null && direccion.Trim().Length >= 6 && direccion.Any(char.IsLetter);

        /// <summary>Busca la dirección dentro del estado de Chihuahua.</summary>
        /// <returns>Las coordenadas (6 decimales, como la BD), o null si no se encontró.</returns>
        /// <exception cref="ReglaNegocioException">Sin internet o el buscador no respondió.</exception>
        public async Task<(decimal Lat, decimal Lng)?> BuscarAsync(string direccion, CancellationToken ct = default)
        {
            string consulta = ArmarConsulta(direccion);
            if (Guardadas.TryGetValue(consulta, out var guardada)) return guardada;

            // Primero tal cual; si no aparece, sin números (Nominatim conoce mejor las calles que los números de casa).
            (decimal Lat, decimal Lng)? punto = await PedirAsync(consulta, ct);
            string sinNumeros = SinNumeros(consulta);
            if (punto == null && sinNumeros != consulta && ValeLaPena(sinNumeros))
                punto = await PedirAsync(sinNumeros, ct);

            Guardadas[consulta] = punto;
            return punto;
        }

        /// <summary>Agrega "Chihuahua, México" si no lo trae, para que no encuentre una calle igual en otro estado.</summary>
        private static string ArmarConsulta(string direccion)
        {
            string limpia = EspaciosDeMas().Replace(direccion.Trim(), " ");
            return limpia.Contains("chihuahua", StringComparison.OrdinalIgnoreCase) ? $"{limpia}, México" : $"{limpia}, Chihuahua, México";
        }

        /// <summary>"Av. Juárez #123 int. 4, Centro" → "Av. Juárez, Centro".</summary>
        private static string SinNumeros(string consulta) =>
            EspaciosDeMas().Replace(NumeroDeCasa().Replace(consulta, ""), " ").Replace(" ,", ",").Trim(' ', ',');

        private async Task<(decimal Lat, decimal Lng)?> PedirAsync(string consulta, CancellationToken ct)
        {
            await Turno.WaitAsync(ct);
            try
            {
                TimeSpan espera = _ultima.AddSeconds(1.1) - DateTime.UtcNow;
                if (espera > TimeSpan.Zero) await Task.Delay(espera, ct);
                _ultima = DateTime.UtcNow;

                string url = "https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&countrycodes=mx&bounded=1"
                    + $"&viewbox={CajaChihuahua}&q={Uri.EscapeDataString(consulta)}";
                using HttpResponseMessage respuesta = await Http.GetAsync(url, ct);
                if (!respuesta.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[Nominatim] {(int)respuesta.StatusCode} al buscar \"{consulta}\"");
                    throw new ReglaNegocioException("El buscador de direcciones no respondió. Marca el punto en el mapa.");
                }

                using JsonDocument json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
                if (json.RootElement.ValueKind != JsonValueKind.Array || json.RootElement.GetArrayLength() == 0) return null;

                JsonElement primero = json.RootElement[0];
                if (!decimal.TryParse(primero.GetProperty("lat").GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal lat)
                    || !decimal.TryParse(primero.GetProperty("lon").GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out decimal lng))
                    return null;

                lat = Math.Round(lat, 6);
                lng = Math.Round(lng, 6);
                return lat is >= Sur and <= Norte && lng is >= Oeste and <= Este ? (lat, lng) : null;
            }
            catch (HttpRequestException)
            {
                throw new ReglaNegocioException("Sin conexión a internet no se puede buscar la dirección. Marca el punto en el mapa.");
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new ReglaNegocioException("El buscador de direcciones tardó demasiado. Marca el punto en el mapa.");
            }
            finally
            {
                Turno.Release();
            }
        }

        [GeneratedRegex(@"\s+")] private static partial Regex EspaciosDeMas();
        /// <summary>"#123", "No. 45", "int. 4", "123-B" y números sueltos.</summary>
        [GeneratedRegex(@"(?i)(#\s*\w+|\b(no|núm|num|int|ext)\.?\s*\d+\w*|\b\d+(-?[a-z])?\b)")] private static partial Regex NumeroDeCasa();
    }
}
