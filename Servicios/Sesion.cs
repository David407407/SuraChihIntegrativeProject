using BackendLogica.Modelos;
using SuraChihIntegrativeProject.Puente;

namespace SuraChihIntegrativeProject.Servicios
{
    /// <summary>
    /// Usuario con sesión iniciada en esta ventana. Por ahora la landing solo tiene el estado
    /// "Visitante"; el inicio de sesión llegará con la pantalla de login.
    /// </summary>
    public sealed class Sesion
    {
        public Usuario? Usuario { get; private set; }

        public bool Iniciada => Usuario != null;

        public void Iniciar(Usuario usuario) => Usuario = usuario;

        public void Cerrar() => Usuario = null;

        /// <summary>Devuelve el usuario o lanza <see cref="RequiereSesionException"/> con el mensaje para la interfaz.</summary>
        public Usuario Requerir(string paraQue) =>
            Usuario ?? throw new RequiereSesionException($"Inicia sesión para {paraQue}.");
    }
}
