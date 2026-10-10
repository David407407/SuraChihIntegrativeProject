using System.Data.Common;
using BackendLogica.Configuracion;
using BackendLogica.Datos;
using BackendLogica.Modelos;
using BackendLogica.Seguridad;

namespace BackendLogica.Repositorios
{
    /// <summary>Cuentas de usuario: registro, inicio de sesión y perfil.</summary>
    public sealed class UsuarioDB : ConexionDB
    {
        private const string Columnas = "id, username, email, avatar_url, is_mod, is_active, created_at";

        public UsuarioDB(ConfiguracionBD? configuracion = null) : base(configuracion) { }

        /// <summary>Crea una cuenta nueva y la devuelve.</summary>
        /// <exception cref="DatosInvalidosException">Nombre, correo o contraseña con formato incorrecto.</exception>
        /// <exception cref="DuplicadoException">El nombre de usuario o el correo ya existen.</exception>
        public async Task<Usuario> RegistrarAsync(string nombreUsuario, string correo, string contrasena, CancellationToken ct = default)
        {
            nombreUsuario = nombreUsuario.Trim();
            correo = correo.Trim().ToLowerInvariant();
            Validar.NombreUsuario(nombreUsuario);
            Validar.Correo(correo);
            Contrasenas.Validar(contrasena);

            int id = await InsertarAsync("""
                INSERT INTO user (username, email, password_hash) VALUES (@nombreUsuario, @correo, @hash)
                """, new { nombreUsuario, correo, hash = Contrasenas.Hashear(contrasena) }, ct);
            return (await ObtenerAsync(id, ct))!;
        }

        /// <summary>
        /// Valida las credenciales. <paramref name="identificador"/> puede ser el correo o el nombre de usuario.
        /// </summary>
        /// <returns>El usuario, o null si las credenciales no coinciden o la cuenta está desactivada.</returns>
        public async Task<Usuario?> IniciarSesionAsync(string identificador, string contrasena, CancellationToken ct = default)
        {
            var fila = await ConsultarUnoAsync($"""
                SELECT {Columnas}, password_hash FROM user
                WHERE (email = @identificador OR username = @identificador) AND is_active
                LIMIT 1
                """, r => (Usuario: Mapear(r), Hash: r.Texto("password_hash")),
                new { identificador = identificador.Trim() }, ct);

            return fila.Usuario != null && Contrasenas.Verificar(contrasena, fila.Hash) ? fila.Usuario : null;
        }

        public Task<Usuario?> ObtenerAsync(int usuarioId, CancellationToken ct = default) =>
            ConsultarUnoAsync($"SELECT {Columnas} FROM user WHERE id = @usuarioId", Mapear, new { usuarioId }, ct);

        public async Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken ct = default) =>
            await EscalarAsync<int>("SELECT COUNT(*) FROM user WHERE username = @nombreUsuario", new { nombreUsuario = nombreUsuario.Trim() }, ct) > 0;

        public async Task<bool> ExisteCorreoAsync(string correo, CancellationToken ct = default) =>
            await EscalarAsync<int>("SELECT COUNT(*) FROM user WHERE email = @correo", new { correo = correo.Trim() }, ct) > 0;

        /// <summary>Edita nombre de usuario, correo y foto (Mi perfil).</summary>
        public async Task<bool> ActualizarPerfilAsync(int usuarioId, string nombreUsuario, string correo, string? avatarUrl, CancellationToken ct = default)
        {
            nombreUsuario = nombreUsuario.Trim();
            correo = correo.Trim().ToLowerInvariant();
            Validar.NombreUsuario(nombreUsuario);
            Validar.Correo(correo);

            return await EjecutarAsync("""
                UPDATE user SET username = @nombreUsuario, email = @correo, avatar_url = @avatarUrl
                WHERE id = @usuarioId AND is_active
                """, new { usuarioId, nombreUsuario, correo, avatarUrl }, ct) > 0;
        }

        /// <returns>false si la contraseña actual no coincide.</returns>
        public async Task<bool> CambiarContrasenaAsync(int usuarioId, string actual, string nueva, CancellationToken ct = default)
        {
            Contrasenas.Validar(nueva);
            string? hash = await EscalarAsync<string>("SELECT password_hash FROM user WHERE id = @usuarioId AND is_active", new { usuarioId }, ct);
            if (hash == null || !Contrasenas.Verificar(actual, hash)) return false;

            await EjecutarAsync("UPDATE user SET password_hash = @hash WHERE id = @usuarioId",
                new { usuarioId, hash = Contrasenas.Hashear(nueva) }, ct);
            return true;
        }

        /// <summary>"Eliminar cuenta": se desactiva para conservar el historial (reseñas, eventos).</summary>
        public async Task<bool> DesactivarAsync(int usuarioId, CancellationToken ct = default) =>
            await EjecutarAsync("UPDATE user SET is_active = FALSE WHERE id = @usuarioId", new { usuarioId }, ct) > 0;

        internal static Usuario Mapear(DbDataReader r) => new(
            r.Entero("id"),
            r.Texto("username"),
            r.Texto("email"),
            r.TextoONulo("avatar_url"),
            r.Bool("is_mod"),
            r.Bool("is_active"),
            r.Fecha("created_at"));
    }
}
