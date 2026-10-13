namespace SuraChihIntegrativeProject.Servicios
{
    /// <summary>
    /// Bandeja de correo de mentira para "Recuperar contraseña".
    /// </summary>
    /// <remarks>
    /// <para>El proyecto todavía no tiene servidor de correo (SMTP). Mientras tanto, el enlace que
    /// "se enviaría" se guarda aquí y el botón "Abrir enlace del correo" del modal "Revisa tu correo"
    /// lo abre directamente, como si el usuario hubiera hecho clic en su correo.</para>
    /// <para>Cuando haya SMTP: en <see cref="Enviar"/> se manda el correo de verdad con el token y
    /// se quita la acción "cuenta.abrirEnlace" de <c>ControladorCuenta</c>. Nada más cambia: el token
    /// y su SHA-256 en la tabla password_reset ya los maneja BackendLogica.</para>
    /// </remarks>
    public sealed class CorreoSimulado
    {
        private string? _ultimoToken;

        /// <summary>"Envía" el enlace de recuperación (aquí solo lo guarda).</summary>
        public void Enviar(string correo, string token)
        {
            _ultimoToken = token;
            System.Diagnostics.Debug.WriteLine($"[CorreoSimulado] Enlace de recuperación para {correo}");
        }

        /// <summary>Devuelve el último enlace "enviado" y lo borra (cada enlace se abre una vez).</summary>
        public string? TomarUltimoToken()
        {
            string? token = _ultimoToken;
            _ultimoToken = null;
            return token;
        }
    }
}
