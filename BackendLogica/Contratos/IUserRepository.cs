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
        Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken ct = default);
        Task<bool> ExisteCorreoAsync(string correo, CancellationToken ct = default);

        /// <summary>Edita nombre de usuario, correo y foto (Mi perfil).</summary>
        Task<bool> ActualizarPerfilAsync(int usuarioId, string nombreUsuario, string correo, string? avatarUrl, CancellationToken ct = default);

        /// <returns>false si la contraseña actual no coincide.</returns>
        Task<bool> CambiarContrasenaAsync(int usuarioId, string actual, string nueva, CancellationToken ct = default);

        /// <summary>"Eliminar cuenta": se desactiva para conservar el historial (reseñas, eventos).</summary>
        Task<bool> DesactivarAsync(int usuarioId, CancellationToken ct = default);
    }
}
