using BackendLogica.Modelos;

namespace BackendLogica.Contratos
{
    /// <summary>
    /// Cuentas de usuario: modales "Crear cuenta", "Iniciar sesión", "Mi perfil" y "Eliminar cuenta".
    /// Implementaciones: <c>UsuarioDB</c> (MySQL) y <c>FalsoUserRepository</c> (memoria).
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>Crea una cuenta nueva y la devuelve.</summary>
        /// <exception cref="Datos.DatosInvalidosException">Nombre, correo o contraseña con formato incorrecto.</exception>
        /// <exception cref="Datos.DuplicadoException">El nombre de usuario o el correo ya existen.</exception>
        Task<Usuario> RegistrarAsync(string nombreUsuario, string correo, string contrasena, CancellationToken ct = default);

        /// <summary><paramref name="identificador"/> puede ser el correo o el nombre de usuario.</summary>
        /// <returns>El usuario, o null si las credenciales no coinciden o la cuenta está desactivada.</returns>
        Task<Usuario?> IniciarSesionAsync(string identificador, string contrasena, CancellationToken ct = default);

        Task<Usuario?> ObtenerAsync(int usuarioId, CancellationToken ct = default);

        /// <summary>
        /// "Continuar con Google": la cuenta con ese correo (ya verificado por Google), activa o no.
        /// Mientras no exista la tabla de cuentas externas, el correo es lo que une las dos cuentas.
        /// </summary>
        Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken ct = default);
        Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken ct = default);
        Task<bool> ExisteCorreoAsync(string correo, CancellationToken ct = default);

        /// <summary>Edita nombre de usuario, correo y foto (Mi perfil).</summary>
        Task<bool> ActualizarPerfilAsync(int usuarioId, string nombreUsuario, string correo, string? avatarUrl, CancellationToken ct = default);

        /// <returns>false si la contraseña actual no coincide.</returns>
        Task<bool> CambiarContrasenaAsync(int usuarioId, string actual, string nueva, CancellationToken ct = default);

        /// <summary>
        /// "Eliminar cuenta": se desactiva (<c>is_active = FALSE</c>) para conservar el historial (reseñas,
        /// eventos) y se invalidan sus enlaces de "Recuperar contraseña" pendientes.
        /// Un moderador no puede usarlo: su cuenta se elimina con <see cref="DesactivarModeradorAsync"/>.
        /// </summary>
        /// <returns>false si la cuenta no existe, ya estaba desactivada o es de un moderador.</returns>
        Task<bool> DesactivarAsync(int usuarioId, CancellationToken ct = default);

        /// <summary>"Equipo de moderación": moderadores activos, primero los supermoderadores.</summary>
        Task<List<Usuario>> ListarModeradoresAsync(CancellationToken ct = default);

        /// <summary>
        /// Un supermoderador elimina la cuenta de un moderador (igual que <see cref="DesactivarAsync"/>:
        /// se desactiva). Las cuentas de supermoderador no se eliminan desde la app.
        /// </summary>
        /// <exception cref="Datos.ReglaNegocioException"><paramref name="superModeradorId"/> no es supermoderador.</exception>
        /// <returns>false si no es la cuenta activa de un moderador.</returns>
        Task<bool> DesactivarModeradorAsync(int superModeradorId, int moderadorId, CancellationToken ct = default);
    }
}
