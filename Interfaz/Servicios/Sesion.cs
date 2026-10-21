using BackendLogica.Modelos;

namespace Interfaz.Servicios
{
    /// <summary>
    /// Usuario con sesión iniciada en esta ventana. La inician y la cierran los modales de cuenta
    /// (ver <see cref="ServicioCuenta"/>); vive mientras la app está abierta.
    /// </summary>
    /// <remarks>
    /// Los componentes que cambian con la sesión (navbar, saludo del Home, Mi perfil...) se suscriben a
    /// <see cref="Cambio"/> y se vuelven a pintar.
    /// </remarks>
    public sealed class Sesion
    {
        public Usuario? Usuario { get; private set; }

        /// <summary>Organizador aprobado: el menú de cuenta muestra "Panel de organizador".</summary>
        public bool EsOrganizador { get; private set; }

        public bool Iniciada => Usuario != null;

        /// <summary>Se inició, se cerró o cambiaron los datos del usuario (nombre, correo).</summary>
        public event Action? Cambio;

        public void Iniciar(Usuario usuario, bool esOrganizador = false)
        {
            Usuario = usuario;
            EsOrganizador = esOrganizador;
            Cambio?.Invoke();
        }

        public void Cerrar()
        {
            if (Usuario == null) return;
            Usuario = null;
            EsOrganizador = false;
            Cambio?.Invoke();
        }

        /// <summary>Devuelve el usuario o lanza <see cref="RequiereSesionException"/> con el mensaje para la interfaz.</summary>
        public Usuario Requerir(string paraQue) =>
            Usuario ?? throw new RequiereSesionException($"Inicia sesión para {paraQue}.");
    }
}
