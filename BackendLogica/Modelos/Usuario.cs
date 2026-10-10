namespace BackendLogica.Modelos
{
    /// <summary>
    /// Cuenta de usuario. Nunca incluye el hash de la contraseña: ese dato no sale de <c>UsuarioDB</c>.
    /// </summary>
    public sealed record Usuario(
        int Id,
        string NombreUsuario,
        string Correo,
        string? AvatarUrl,
        bool EsModerador,
        bool Activo,
        DateTime CreadoEn);

    /// <summary>Perfil (y solicitud) de organizador. Organizador = usuario con estado Aprobado.</summary>
    public sealed record PerfilOrganizador(
        int UsuarioId,
        string NombreNegocio,
        string? Descripcion,
        string RedSocialUrl,
        string WhatsApp,
        EstadoOrganizador Estado,
        bool Verificado,
        DateTime SolicitadoEn);

    /// <summary>Datos que llena el usuario en "Hazte organizador".</summary>
    /// <param name="WhatsApp">Solo dígitos con lada, 10 a 15 caracteres. Ej.: 526141234567.</param>
    public sealed record SolicitudOrganizador(
        int UsuarioId,
        string NombreNegocio,
        string? Descripcion,
        string RedSocialUrl,
        string WhatsApp);
}
