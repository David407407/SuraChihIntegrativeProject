using BackendLogica.Datos;
using BackendLogica.Seguridad;

namespace Interfaz.Utilidades
{
    /// <summary>
    /// Reglas de los formularios de cuenta (modales y Mi perfil). Son las mismas funciones de
    /// BackendLogica (<see cref="Validar"/> y <see cref="Contrasenas"/>, que siguen el CHECK
    /// ck_user_username), así que el mensaje que se ve antes de enviar es idéntico al que daría C# al guardar.
    /// </summary>
    public static class ReglasCuenta
    {
        public const string NoCoinciden = "Las contraseñas no coinciden.";

        /// <summary>Tamaños de user.first_name y user.last_name.</summary>
        public const int MaximoNombre = Validar.MaximoNombre, MaximoApellidos = Validar.MaximoApellidos;

        /// <summary>min del calendario de "Fecha de nacimiento" (hace 120 años), como "aaaa-mm-dd".</summary>
        public static string NacimientoMinimo => DateTime.Today.AddYears(-120).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>max del calendario: hoy menos la edad mínima (13 años).</summary>
        public static string NacimientoMaximo => DateTime.Today.AddYears(-Validar.EdadMinima).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        public static string? ErrorNombre(string valor) => Revisar(() => Validar.Nombre(valor));

        public static string? ErrorApellidos(string valor) => Revisar(() => Validar.Apellidos(valor));

        public static string? ErrorNacimiento(DateOnly fecha) => Revisar(() => Validar.FechaNacimiento(fecha));

        /// <returns>El mensaje de error, o null si el valor es válido.</returns>
        public static string? ErrorNombreUsuario(string valor) => Revisar(() => Validar.NombreUsuario(valor));

        public static string? ErrorCorreo(string valor) => Revisar(() => Validar.Correo(valor));

        /// <summary>8+ caracteres con letras y números.</summary>
        public static string? ErrorContrasena(string valor) => Revisar(() => Contrasenas.Validar(valor));

        /// <summary>Solo dígitos con lada, 10 a 15 (ck_orgprof_whatsapp).</summary>
        public static string? ErrorWhatsApp(string valor) => Revisar(() => Validar.WhatsApp(valor));

        /// <summary>RFC ya limpio (mayúsculas, sin espacios): 12 o 13 caracteres (ck_orgprof_rfc).</summary>
        public static string? ErrorRfc(string valor) => Revisar(() => Validar.Rfc(valor));

        public static bool EsCorreo(string? valor) => valor != null && ErrorCorreo(valor) == null;

        private static string? Revisar(Action validar)
        {
            try
            {
                validar();
                return null;
            }
            catch (DatosInvalidosException ex)
            {
                return ex.Message;
            }
        }
    }
}
