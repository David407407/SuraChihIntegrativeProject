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

        /// <returns>El mensaje de error, o null si el valor es válido.</returns>
        public static string? ErrorNombreUsuario(string valor) => Revisar(() => Validar.NombreUsuario(valor));

        public static string? ErrorCorreo(string valor) => Revisar(() => Validar.Correo(valor));

        /// <summary>8+ caracteres con letras y números.</summary>
        public static string? ErrorContrasena(string valor) => Revisar(() => Contrasenas.Validar(valor));

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
