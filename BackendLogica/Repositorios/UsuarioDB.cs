using System.Data.Common;
using BackendLogica.Configuracion;
using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;
using BackendLogica.Seguridad;

namespace BackendLogica.Repositorios
{
    /// <summary>Cuentas de usuario: registro, inicio de sesión y perfil.</summary>
    public sealed class UsuarioDB : ConexionDB, IUserRepository
    {
        private const string Columnas = "id, username, email, avatar_url, is_mod, is_super_mod, is_active, created_at";

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

        public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken ct = default) =>
            ConsultarUnoAsync($"SELECT {Columnas} FROM user WHERE email = @correo LIMIT 1", Mapear,
                new { correo = correo.Trim().ToLowerInvariant() }, ct);

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

        /// <summary>
        /// "Eliminar cuenta": <c>is_active = FALSE</c> (nunca DELETE, para conservar reseñas y eventos) e
        /// invalida los enlaces de "Recuperar contraseña" que estuvieran pendientes. Un moderador no puede
        /// eliminar su propia cuenta: la elimina un supermoderador (<see cref="DesactivarModeradorAsync"/>).
        /// </summary>
        /// <returns>false si la cuenta no existe, ya estaba desactivada o es de un moderador.</returns>
        public Task<bool> DesactivarAsync(int usuarioId, CancellationToken ct = default) =>
            DesactivarDondeAsync(usuarioId, "NOT is_mod", ct);

        /// <summary>"Equipo de moderación": moderadores activos, primero los supermoderadores.</summary>
        public Task<List<Usuario>> ListarModeradoresAsync(CancellationToken ct = default) =>
            ConsultarAsync($"""
                SELECT {Columnas} FROM user WHERE is_mod AND is_active ORDER BY is_super_mod DESC, username
                """, Mapear, ct: ct);

        /// <summary>Un supermoderador elimina la cuenta de un moderador. Las de supermoderador no se eliminan aquí.</summary>
        /// <exception cref="ReglaNegocioException"><paramref name="superModeradorId"/> no es supermoderador.</exception>
        /// <returns>false si no es la cuenta activa de un moderador.</returns>
        public async Task<bool> DesactivarModeradorAsync(int superModeradorId, int moderadorId, CancellationToken ct = default)
        {
            if (await ObtenerAsync(superModeradorId, ct) is not { EsSuperModerador: true, Activo: true })
                throw new ReglaNegocioException(SoloSuperModeradores);
            return await DesactivarDondeAsync(moderadorId, "is_mod AND NOT is_super_mod", ct);
        }

        /// <summary>Mismo mensaje en <c>FalsoUserRepository</c>.</summary>
        internal const string SoloSuperModeradores = "Solo un supermoderador puede eliminar cuentas de moderador.";

        /// <param name="condicion">Texto constante del código (nunca viene del usuario).</param>
        private Task<bool> DesactivarDondeAsync(int usuarioId, string condicion, CancellationToken ct) =>
            EnTransaccionAsync(async tx =>
            {
                int filas = await tx.EjecutarAsync($"UPDATE user SET is_active = FALSE WHERE id = @usuarioId AND is_active AND {condicion}", new { usuarioId });
                if (filas == 0) return false;

                await tx.EjecutarAsync("""
                    UPDATE password_reset SET used_at = NOW() WHERE user_id = @usuarioId AND used_at IS NULL
                    """, new { usuarioId });
                return true;
            }, ct);

        internal static Usuario Mapear(DbDataReader r) => new(
            r.Entero("id"),
            r.Texto("username"),
            r.Texto("email"),
            r.TextoONulo("avatar_url"),
            r.Bool("is_mod"),
            r.Bool("is_active"),
            r.Fecha("created_at"),
            r.Bool("is_super_mod"));
    }
}
