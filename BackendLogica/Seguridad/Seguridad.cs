using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BackendLogica.Datos;

namespace BackendLogica.Seguridad
{
    /// <summary>Hash de contraseñas con BCrypt (BCrypt.Net-Next). La BD solo guarda el hash.</summary>
    public static class Contrasenas
    {
        private const int Costo = 11;   // el mismo de los datos de prueba ($2b$11$...)
        public const int LongitudMinima = 8;

        public static string Hashear(string contrasena) => BCrypt.Net.BCrypt.HashPassword(contrasena, Costo);

        public static bool Verificar(string contrasena, string hash)
        {
            try { return BCrypt.Net.BCrypt.Verify(contrasena, hash); }
            catch (BCrypt.Net.SaltParseException) { return false; }   // hash corrupto o de otro formato
        }

        /// <summary>Lanza <see cref="DatosInvalidosException"/> si la contraseña es demasiado débil.</summary>
        public static void Validar(string contrasena)
        {
            if (contrasena.Length < LongitudMinima)
                throw new DatosInvalidosException($"La contraseña debe tener al menos {LongitudMinima} caracteres.");
            if (!contrasena.Any(char.IsLetter) || !contrasena.Any(char.IsDigit))
                throw new DatosInvalidosException("La contraseña debe combinar letras y números.");
        }
    }

    /// <summary>Tokens de un solo uso (recuperar contraseña). Se envía el token y se guarda solo su SHA-256.</summary>
    public static class Tokens
    {
        /// <summary>Token aleatorio seguro para URL (43 caracteres).</summary>
        public static string Generar() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        /// <summary>SHA-256 en hexadecimal (64 caracteres), igual que la columna password_reset.token_hash.</summary>
        public static string Hashear(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }

    /// <summary>
    /// Validaciones que replican los CHECK de la BD, para dar mensajes claros antes de llegar a MySQL.
    /// </summary>
    public static partial class Validar
    {
        [GeneratedRegex("^[A-Za-z0-9_.]{3,30}$")] private static partial Regex NombreUsuarioRegex();
        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")] private static partial Regex CorreoRegex();
        [GeneratedRegex("^[0-9]{10,15}$")] private static partial Regex WhatsAppRegex();
        /// <summary>Letras (con acentos), espacios, apóstrofo, punto y guion: "María José", "De la O", "Pérez-Ruiz".</summary>
        [GeneratedRegex(@"^[\p{L}][\p{L} .'\-]*$")] private static partial Regex NombrePersonaRegex();
        /// <summary>RFC del SAT: 3 (persona moral) o 4 (persona física) letras, fecha AAMMDD y homoclave de 3.</summary>
        [GeneratedRegex("^[A-ZÑ&]{3,4}[0-9]{6}[A-Z0-9]{3}$")] private static partial Regex RfcRegex();

        /// <summary>Máximos de las columnas user.first_name / user.last_name.</summary>
        public const int MaximoNombre = 60, MaximoApellidos = 80;
        /// <summary>Edad mínima para crear cuenta.</summary>
        public const int EdadMinima = 13;
        /// <summary>Videos del local en "Hazte organizador".</summary>
        public const int MinimoVideos = 1, MaximoVideos = 3;

        /// <summary>Nombre(s) de la persona: 2 a 60 caracteres, solo letras.</summary>
        public static void Nombre(string valor)
        {
            valor = valor.Trim();
            if (valor.Length < 2 || valor.Length > MaximoNombre || !NombrePersonaRegex().IsMatch(valor))
                throw new DatosInvalidosException($"El nombre debe tener de 2 a {MaximoNombre} letras.");
        }

        /// <summary>Apellidos: 2 a 80 caracteres, solo letras.</summary>
        public static void Apellidos(string valor)
        {
            valor = valor.Trim();
            if (valor.Length < 2 || valor.Length > MaximoApellidos || !NombrePersonaRegex().IsMatch(valor))
                throw new DatosInvalidosException($"Los apellidos deben tener de 2 a {MaximoApellidos} letras.");
        }

        /// <summary>No puede ser futura, hay que tener al menos <see cref="EdadMinima"/> años y no más de 120.</summary>
        public static void FechaNacimiento(DateOnly fecha)
        {
            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);
            if (fecha > hoy || fecha < hoy.AddYears(-120))
                throw new DatosInvalidosException("La fecha de nacimiento no es válida.");
            if (fecha > hoy.AddYears(-EdadMinima))
                throw new DatosInvalidosException($"La fecha de nacimiento indica que tienes menos de {EdadMinima} años.");
        }

        public static void DatosPersonales(Modelos.DatosPersonales datos)
        {
            Nombre(datos.Nombre);
            Apellidos(datos.Apellidos);
            FechaNacimiento(datos.FechaNacimiento);
        }

        /// <summary>RFC ya en mayúsculas y sin espacios (ck_orgprof_rfc).</summary>
        public static void Rfc(string? valor)
        {
            if (valor == null || !RfcRegex().IsMatch(valor))
                throw new DatosInvalidosException("El RFC debe tener 12 caracteres (persona moral) o 13 (persona física). Ej.: GODE561231GR8.");
        }

        /// <summary>Videos del local: de 1 a 3 URL https (secure_url de Cloudinary).</summary>
        public static void Videos(IReadOnlyList<string>? urls)
        {
            if (urls == null || urls.Count < MinimoVideos)
                throw new DatosInvalidosException("Sube al menos un video de tu local.");
            if (urls.Count > MaximoVideos)
                throw new DatosInvalidosException($"Puedes subir hasta {MaximoVideos} videos.");
            foreach (string url in urls)
                if (url.Length > 500 || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps)
                    throw new DatosInvalidosException("Cada video debe ser una URL https (la secure_url que devuelve Cloudinary).");
        }

        public static void NombreUsuario(string valor)
        {
            if (!NombreUsuarioRegex().IsMatch(valor))
                throw new DatosInvalidosException("El nombre de usuario debe tener de 3 a 30 caracteres: letras, números, punto o guion bajo.");
        }

        public static void Correo(string valor)
        {
            if (valor.Length > 254 || !CorreoRegex().IsMatch(valor))
                throw new DatosInvalidosException("Escribe un correo válido.");
        }

        public static void WhatsApp(string valor)
        {
            if (!WhatsAppRegex().IsMatch(valor))
                throw new DatosInvalidosException("El WhatsApp debe tener solo dígitos con lada (10 a 15). Ej.: 526141234567.");
        }

        public static void Calificacion(int estrellas)
        {
            if (estrellas is < 1 or > 5)
                throw new DatosInvalidosException("La calificación debe ser de 1 a 5 estrellas.");
        }

        public static void Requerido(string? valor, string campo)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new DatosInvalidosException($"{campo} es obligatorio.");
        }

        /// <summary>Caja aproximada del estado de Chihuahua (igual que ck_place_lat / ck_place_lng).</summary>
        public static void CoordenadasChihuahua(decimal latitud, decimal longitud)
        {
            if (latitud is < 25.5m or > 31.8m || longitud is < -109.2m or > -103.3m)
                throw new DatosInvalidosException("La ubicación debe estar dentro del estado de Chihuahua.");
        }

        /// <summary>Foto subida a Cloudinary: su <c>secure_url</c> (https, hasta 500 caracteres como <c>event_image.url</c>).</summary>
        public static void UrlImagen(string? url)
        {
            if (url == null || url.Length > 500
                || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new DatosInvalidosException("Cada foto debe ser una URL https (la secure_url que devuelve Cloudinary).");
        }

        public static void RangoPrecio(decimal? minimo, decimal? maximo)
        {
            if (minimo < 0 || (minimo.HasValue && maximo.HasValue && minimo > maximo))
                throw new DatosInvalidosException("El precio mínimo no puede ser mayor que el máximo.");
        }
    }
}
