namespace BackendLogica.Modelos
{
    /// <summary>
    /// Cuenta de usuario. Nunca incluye el hash de la contraseña: ese dato no sale de <c>UsuarioDB</c>.
    /// </summary>
    /// <param name="EsSuperModerador">Es el único que elimina cuentas de moderador. Siempre es también moderador.</param>
    public sealed record Usuario(
        int Id,
        string NombreUsuario,
        string Correo,
        string? AvatarUrl,
        bool EsModerador,
        bool Activo,
        DateTime CreadoEn,
        bool EsSuperModerador = false);

    /// <summary>Datos personales que pide "Crear cuenta" (columnas first_name, last_name y birth_date de user).</summary>
    public sealed record DatosPersonales(string Nombre, string Apellidos, DateOnly FechaNacimiento);

    /// <summary>Perfil (y solicitud) de organizador. Organizador = usuario con estado Aprobado.</summary>
    /// <param name="Rfc">RFC del negocio (null en solicitudes anteriores a este campo).</param>
    /// <param name="Videos">URL de los videos del local (tabla organizer_video), en orden.</param>
    public sealed record PerfilOrganizador(
        int UsuarioId,
        string NombreNegocio,
        string? Descripcion,
        string RedSocialUrl,
        string WhatsApp,
        EstadoOrganizador Estado,
        bool Verificado,
        DateTime SolicitadoEn,
        string? Rfc = null,
        IReadOnlyList<string>? Videos = null);

    /// <summary>Datos que llena el usuario en "Hazte organizador".</summary>
    /// <param name="WhatsApp">Solo dígitos con lada, 10 a 15 caracteres. Ej.: 526141234567.</param>
    /// <param name="Rfc">RFC del negocio en mayúsculas (12 o 13 caracteres). Obligatorio en solicitudes nuevas.</param>
    /// <param name="Videos">URL https de Cloudinary de los videos del local (1 a 3).</param>
    public sealed record SolicitudOrganizador(
        int UsuarioId,
        string NombreNegocio,
        string? Descripcion,
        string RedSocialUrl,
        string WhatsApp,
        string? Rfc = null,
        IReadOnlyList<string>? Videos = null);
}
