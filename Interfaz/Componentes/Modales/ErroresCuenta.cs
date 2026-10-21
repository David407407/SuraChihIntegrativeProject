using System.Text.RegularExpressions;

namespace Interfaz.Componentes.Modales
{
    /// <summary>
    /// En qué campo pintar un error de los formularios de cuenta. Los de BackendLogica no dicen el campo,
    /// así que se deduce del texto ("Ese correo ya está registrado.", "El nombre de usuario...").
    /// </summary>
    public static class ErroresCuenta
    {
        /// <param name="mensaje">Texto del error tal como lo dio C#.</param>
        /// <param name="campos">Campos que tiene el formulario.</param>
        /// <returns>El campo, o null si no es de ninguno (va como aviso general).</returns>
        public static string? CampoDe(string mensaje, params string[] campos)
        {
            string? Existe(string campo) => campos.Contains(campo) ? campo : null;
            bool Dice(string patron) => Regex.IsMatch(mensaje, patron, RegexOptions.IgnoreCase);

            if (Dice("no coinciden")) return Existe("confirmacion");
            if (Dice("incorrect")) return Existe("contrasena");
            if (Dice("contraseña")) return Existe("contrasena");
            if (Dice("nombre de usuario")) return Existe("nombreUsuario");
            if (Dice("correo")) return Existe("correo") ?? Existe("identificador");
            return null;
        }
    }
}
