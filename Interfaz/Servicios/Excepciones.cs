using BackendLogica.Datos;

namespace Interfaz.Servicios
{
    /// <summary>La acción necesita un usuario con sesión iniciada (se abre "Inicia sesión para continuar").</summary>
    public sealed class RequiereSesionException(string mensaje) : Exception(mensaje);

    public static class MensajesError
    {
        /// <summary>
        /// Texto para mostrar al usuario. Los errores de BackendLogica (<see cref="SuraChihException"/>) ya
        /// traen un mensaje listo; los demás son fallas de programación y no se enseñan tal cual.
        /// </summary>
        public static string Para(Exception ex) => ex switch
        {
            RequiereSesionException or SuraChihException => ex.Message,
            _ => "Algo salió mal. Intenta de nuevo."
        };
    }
}
