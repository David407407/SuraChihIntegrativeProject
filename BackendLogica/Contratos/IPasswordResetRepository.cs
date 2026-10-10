namespace BackendLogica.Contratos
{
    /// <summary>
    /// Modales "Recuperar contraseña", "Revisa tu correo" y "Contraseña nueva".
    /// Implementaciones: <c>RecuperacionContrasenaDB</c> y <c>FalsoPasswordResetRepository</c>.
    /// </summary>
    public interface IPasswordResetRepository
    {
        /// <summary>Crea un token de un solo uso para el correo e invalida los anteriores.</summary>
        /// <returns>
        /// El token para el correo, o null si no hay una cuenta activa con ese correo. La pantalla debe
        /// mostrar el mismo mensaje en ambos casos para no revelar qué correos existen.
        /// </returns>
        Task<string?> SolicitarAsync(string correo, CancellationToken ct = default);

        /// <returns>false si el token no existe, ya se usó o expiró.</returns>
        Task<bool> RestablecerAsync(string token, string nuevaContrasena, CancellationToken ct = default);
    }
}
