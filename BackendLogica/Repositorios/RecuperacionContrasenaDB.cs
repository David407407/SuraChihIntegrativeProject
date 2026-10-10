using BackendLogica.Configuracion;
using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Seguridad;

namespace BackendLogica.Repositorios
{
    /// <summary>
    /// "¿Olvidaste tu contraseña?". Genera un token de un solo uso que se manda por correo;
    /// la BD solo guarda su SHA-256, así que un respaldo filtrado no sirve para entrar.
    /// </summary>
    public sealed class RecuperacionContrasenaDB : ConexionDB, IPasswordResetRepository
    {
        /// <summary>Tiempo que dura válido el enlace del correo.</summary>
        public static readonly TimeSpan Vigencia = TimeSpan.FromHours(1);

        public RecuperacionContrasenaDB(ConfiguracionBD? configuracion = null) : base(configuracion) { }

        /// <summary>
        /// Crea un token para el correo indicado e invalida los anteriores.
        /// </summary>
        /// <returns>
        /// El token para incluir en el correo, o null si no hay una cuenta activa con ese correo.
        /// La pantalla debe mostrar el mismo mensaje en ambos casos para no revelar qué correos existen.
        /// </returns>
        public Task<string?> SolicitarAsync(string correo, CancellationToken ct = default) =>
            EnTransaccionAsync(async tx =>
            {
                int? usuarioId = await tx.EscalarAsync<int?>(
                    "SELECT id FROM user WHERE email = @correo AND is_active", new { correo = correo.Trim() });
                if (usuarioId == null) return null;

                await tx.EjecutarAsync("""
                    UPDATE password_reset SET used_at = NOW() WHERE user_id = @usuarioId AND used_at IS NULL
                    """, new { usuarioId });

                string token = Tokens.Generar();
                await tx.EjecutarAsync("""
                    INSERT INTO password_reset (user_id, token_hash, expires_at)
                    VALUES (@usuarioId, @hash, @expira)
                    """, new { usuarioId, hash = Tokens.Hashear(token), expira = DateTime.Now.Add(Vigencia) });
                return (string?)token;
            }, ct);

        /// <summary>Cambia la contraseña si el token existe, no se ha usado y no ha expirado.</summary>
        /// <returns>false si el token no es válido.</returns>
        public Task<bool> RestablecerAsync(string token, string nuevaContrasena, CancellationToken ct = default)
        {
            Contrasenas.Validar(nuevaContrasena);
            return EnTransaccionAsync(async tx =>
            {
                var fila = (await tx.ConsultarAsync("""
                    SELECT id, user_id FROM password_reset
                    WHERE token_hash = @hash AND used_at IS NULL AND expires_at > NOW()
                    FOR UPDATE
                    """, r => (Id: r.Entero("id"), UsuarioId: r.Entero("user_id")),
                    new { hash = Tokens.Hashear(token.Trim()) })).FirstOrDefault();
                if (fila.Id == 0) return false;

                await tx.EjecutarAsync("UPDATE user SET password_hash = @hash WHERE id = @usuarioId",
                    new { usuarioId = fila.UsuarioId, hash = Contrasenas.Hashear(nuevaContrasena) });
                await tx.EjecutarAsync("UPDATE password_reset SET used_at = NOW() WHERE id = @id", new { id = fila.Id });
                return true;
            }, ct);
        }
    }
}
