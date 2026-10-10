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

        public static void RangoPrecio(decimal? minimo, decimal? maximo)
        {
            if (minimo < 0 || (minimo.HasValue && maximo.HasValue && minimo > maximo))
                throw new DatosInvalidosException("El precio mínimo no puede ser mayor que el máximo.");
        }
    }
}
