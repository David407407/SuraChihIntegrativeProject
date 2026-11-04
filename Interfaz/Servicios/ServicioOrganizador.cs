using BackendLogica.Contratos;
using BackendLogica.Modelos;
using Interfaz.Utilidades;

namespace Interfaz.Servicios
{
    /// <summary>
    /// Pantalla "Organizador / Hazte organizador" (Paginas/HazteOrganizador.razor) y su modal "Solicitud enviada".
    /// Tabla <c>organizer_profile</c>; la decisión del moderador se lee de <c>moderation</c> (<c>organizer_id</c>).
    /// </summary>
    /// <remarks>
    /// Lo que el moderador ve y decide está en <see cref="ServicioModeracion"/>. La regla de que nadie aprueba su
    /// propia solicitud la cuida la BD (trigger <c>trg_moderation_bi</c>).
    /// </remarks>
    public sealed class ServicioOrganizador(ISuraChihBD bd, Sesion sesion)
    {
        /// <summary>Tamaños de las columnas de <c>organizer_profile</c>.</summary>
        public const int MaximoNombre = 120;
        public const int MaximoDescripcion = 500;
        public const int MaximoRedSocial = 500;

        /// <summary>
        /// La solicitud del usuario y la última decisión sobre ella. Si un moderador la decidió mientras la sesión
        /// estaba abierta, actualiza <see cref="Sesion.EsOrganizador"/> (y con él el menú de cuenta).
        /// </summary>
        public async Task<DatosSolicitud> ObtenerAsync()
        {
            Usuario usuario = sesion.Requerir("solicitar ser organizador");
            var perfil = bd.Organizadores.ObtenerAsync(usuario.Id);
            var decision = bd.Moderacion.ObtenerUltimaDecisionAsync(TipoObjetivoModeracion.Organizador, usuario.Id);
            await Task.WhenAll(perfil, decision);

            bool aprobado = perfil.Result?.Estado == EstadoOrganizador.Aprobado;
            if (aprobado != sesion.EsOrganizador) sesion.Iniciar(usuario, aprobado);
            return new DatosSolicitud(perfil.Result, decision.Result);
        }

        /// <summary>"Enviar solicitud". Si la anterior fue rechazada, BackendLogica la reenvía con estos datos.</summary>
        public Task EnviarAsync(string nombre, string? descripcion, string redSocial, string whatsApp)
        {
            Usuario usuario = sesion.Requerir("solicitar ser organizador");
            return bd.Organizadores.SolicitarAsync(new SolicitudOrganizador(
                usuario.Id,
                nombre.Trim(),
                string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
                Formato.UrlPublica(redSocial),
                SoloDigitos(whatsApp)));
        }

        /// <summary>"52 614 111 0002" → "526141110002", como lo pide la BD. Si trae letras se deja igual para que no pase.</summary>
        public static string SoloDigitos(string whatsApp) =>
            new([.. whatsApp.Where(c => !char.IsWhiteSpace(c) && c is not ('+' or '-' or '(' or ')'))]);

        /// <summary>Errores del formulario por campo, con los mismos mensajes que daría BackendLogica al guardar.</summary>
        public static IEnumerable<(string Campo, string Mensaje)> Revisar(string? nombre, string? descripcion, string? redSocial, string? whatsApp)
        {
            if (string.IsNullOrWhiteSpace(nombre)) yield return ("nombre", "Escribe el nombre de tu negocio o colectivo.");
            else if (nombre.Trim().Length > MaximoNombre) yield return ("nombre", $"El nombre puede tener hasta {MaximoNombre} caracteres.");

            if (descripcion?.Trim().Length > MaximoDescripcion) yield return ("descripcion", $"La descripción puede tener hasta {MaximoDescripcion} caracteres.");

            if (string.IsNullOrWhiteSpace(redSocial)) yield return ("redSocial", "Escribe el enlace a tu red social o sitio.");
            else if (redSocial.Trim().Length > MaximoRedSocial || !EsEnlace(redSocial)) yield return ("redSocial", "Escribe un enlace válido, por ejemplo instagram.com/tu_negocio.");

            if (ReglasCuenta.ErrorWhatsApp(SoloDigitos(whatsApp ?? "")) is { } error) yield return ("whatsApp", error);
        }

        private static bool EsEnlace(string valor) =>
            !valor.Trim().Any(char.IsWhiteSpace)
            && Uri.TryCreate(Formato.UrlPublica(valor), UriKind.Absolute, out var uri)
            && uri.Host.Contains('.');
    }

    /// <param name="Perfil">null si todavía no ha enviado una solicitud.</param>
    /// <param name="Decision">La última decisión de moderación (su comentario es el motivo del rechazo).</param>
    public sealed record DatosSolicitud(PerfilOrganizador? Perfil, RegistroModeracion? Decision);
}
