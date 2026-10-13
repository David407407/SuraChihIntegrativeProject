using BackendLogica.Modelos;
using SuraChihIntegrativeProject.Puente;

namespace SuraChihIntegrativeProject.Servicios
{
    /// <summary>
    /// Usuario con sesión iniciada en esta ventana. La inician y la cierran los modales de cuenta
    /// (ver <c>Controladores/ControladorCuenta</c>); vive mientras la app está abierta.
    /// </summary>
    public sealed class Sesion
    {
        public Usuario? Usuario { get; private set; }

        /// <summary>Organizador aprobado: el menú de cuenta muestra "Panel de organizador".</summary>
        public bool EsOrganizador { get; private set; }

        public bool Iniciada => Usuario != null;

        public void Iniciar(Usuario usuario, bool esOrganizador = false)
        {
            Usuario = usuario;
            EsOrganizador = esOrganizador;
        }

        public void Cerrar()
        {
            Usuario = null;
            EsOrganizador = false;
        }

        /// <summary>Devuelve el usuario o lanza <see cref="RequiereSesionException"/> con el mensaje para la interfaz.</summary>
        public Usuario Requerir(string paraQue) =>
            Usuario ?? throw new RequiereSesionException($"Inicia sesión para {paraQue}.");

        /// <summary>Lo que la interfaz necesita del usuario (navbar y menú de cuenta), o null si no hay sesión.</summary>
        public UsuarioSesion? ParaInterfaz() =>
            Usuario is { } u ? new UsuarioSesion(u.Id, u.NombreUsuario, u.Correo, u.AvatarUrl, u.EsModerador, EsOrganizador) : null;
    }

    /// <summary>
    /// Respuesta de "sesion.obtener", "cuenta.iniciarSesion" y "cuenta.registrar".
    /// Mismos nombres que <see cref="Usuario"/> para que el navbar lo lea igual, más <c>EsOrganizador</c>.
    /// </summary>
    public sealed record UsuarioSesion(
        int Id,
        string NombreUsuario,
        string Correo,
        string? AvatarUrl,
        bool EsModerador,
        bool EsOrganizador);
}
