using System.Data.Common;
using BackendLogica.Configuracion;
using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;
using BackendLogica.Seguridad;

namespace BackendLogica.Repositorios
{
    /// <summary>
    /// Solicitudes y perfiles de organizador. Un organizador es un usuario normal cuya solicitud
    /// fue aprobada por un moderador (ver <see cref="ModeracionDB"/>).
    /// </summary>
    public sealed class OrganizadorDB : ConexionDB, IOrganizerRepository
    {
        public OrganizadorDB(ConfiguracionBD? configuracion = null) : base(configuracion) { }

        /// <summary>
        /// "Hazte organizador". Si una solicitud anterior fue rechazada, se reenvía con los datos nuevos.
        /// </summary>
        /// <exception cref="DuplicadoException">Ya tiene una solicitud pendiente o ya es organizador.</exception>
        public Task SolicitarAsync(SolicitudOrganizador s, CancellationToken ct = default)
        {
            Validar.Requerido(s.NombreNegocio, "El nombre del negocio");
            Validar.Requerido(s.RedSocialUrl, "El enlace a tu red social");
            Validar.WhatsApp(s.WhatsApp);
            if (s.Rfc != null) Validar.Rfc(s.Rfc);
            if (s.Videos != null) Validar.Videos(s.Videos);

            var parametros = new
            {
                s.UsuarioId, NombreNegocio = s.NombreNegocio.Trim(), s.Descripcion,
                RedSocialUrl = s.RedSocialUrl.Trim(), s.WhatsApp, s.Rfc
            };

            return EnTransaccionAsync(async tx =>
            {
                string? estado = await tx.EscalarAsync<string>(
                    "SELECT status FROM organizer_profile WHERE user_id = @UsuarioId FOR UPDATE", parametros);

                if (estado == null)
                {
                    await tx.EjecutarAsync("""
                        INSERT INTO organizer_profile (user_id, business_name, description, social_url, whatsapp, rfc)
                        VALUES (@UsuarioId, @NombreNegocio, @Descripcion, @RedSocialUrl, @WhatsApp, @Rfc)
                        """, parametros);
                }
                else
                {
                    if (EnumBD.Leer<EstadoOrganizador>(estado) != EstadoOrganizador.Rechazado)
                        throw new DuplicadoException("Ya enviaste una solicitud de organizador.");

                    await tx.EjecutarAsync("""
                        UPDATE organizer_profile
                        SET business_name = @NombreNegocio, description = @Descripcion, social_url = @RedSocialUrl,
                            whatsapp = @WhatsApp, rfc = @Rfc, status = 'pending', requested_at = NOW()
                        WHERE user_id = @UsuarioId
                        """, parametros);
                }

                // Videos del local: se reemplazan por los de esta solicitud (al reenviar una rechazada también).
                if (s.Videos != null)
                {
                    await tx.EjecutarAsync("DELETE FROM organizer_video WHERE user_id = @UsuarioId", parametros);
                    for (int i = 0; i < s.Videos.Count; i++)
                        await tx.EjecutarAsync("""
                            INSERT INTO organizer_video (user_id, url, position) VALUES (@usuarioId, @url, @posicion)
                            """, new { usuarioId = s.UsuarioId, url = s.Videos[i], posicion = i });
                }
                return 0;
            }, ct);
        }

        public async Task<PerfilOrganizador?> ObtenerAsync(int usuarioId, CancellationToken ct = default)
        {
            PerfilOrganizador? perfil = await ConsultarUnoAsync("SELECT * FROM organizer_profile WHERE user_id = @usuarioId", Mapear, new { usuarioId }, ct);
            return perfil == null ? null : (await ConVideosAsync([perfil], ct))[0];
        }

        /// <summary>Puede publicar eventos y registrar lugares.</summary>
        public async Task<bool> EsAprobadoAsync(int usuarioId, CancellationToken ct = default) =>
            await EscalarAsync<int>(
                "SELECT COUNT(*) FROM organizer_profile WHERE user_id = @usuarioId AND status = 'approved'",
                new { usuarioId }, ct) > 0;

        /// <summary>Solicitudes en un estado (ej. Pendiente para la pantalla de moderación), sin las de cuentas eliminadas.</summary>
        public async Task<List<PerfilOrganizador>> ListarPorEstadoAsync(EstadoOrganizador estado, CancellationToken ct = default)
        {
            List<PerfilOrganizador> perfiles = await ConsultarAsync("""
                SELECT op.* FROM organizer_profile op JOIN user u ON u.id = op.user_id
                WHERE op.status = @estado AND u.is_active
                ORDER BY op.requested_at
                """,
                Mapear, new { estado }, ct);
            return await ConVideosAsync(perfiles, ct);
        }

        /// <summary>Agrega a cada perfil sus videos (tabla organizer_video) con una sola consulta.</summary>
        private async Task<List<PerfilOrganizador>> ConVideosAsync(List<PerfilOrganizador> perfiles, CancellationToken ct)
        {
            if (perfiles.Count == 0) return perfiles;
            var filas = await ConsultarAsync("""
                SELECT user_id, url FROM organizer_video WHERE user_id IN @ids ORDER BY user_id, position, id
                """, r => (UsuarioId: r.Entero("user_id"), Url: r.Texto("url")),
                new { ids = perfiles.Select(p => p.UsuarioId).ToList() }, ct);
            var porUsuario = filas.ToLookup(f => f.UsuarioId, f => f.Url);
            return [.. perfiles.Select(p => p with { Videos = [.. porUsuario[p.UsuarioId]] })];
        }

        /// <summary>Edita los datos públicos del organizador (no cambia su estado).</summary>
        public async Task<bool> ActualizarPerfilAsync(int usuarioId, string nombreNegocio, string? descripcion, string whatsApp, CancellationToken ct = default)
        {
            Validar.Requerido(nombreNegocio, "El nombre del negocio");
            Validar.WhatsApp(whatsApp);
            return await EjecutarAsync("""
                UPDATE organizer_profile SET business_name = @nombreNegocio, description = @descripcion, whatsapp = @whatsApp
                WHERE user_id = @usuarioId
                """, new { usuarioId, nombreNegocio = nombreNegocio.Trim(), descripcion, whatsApp }, ct) > 0;
        }

        /// <summary>Badge de verificado (moderadores, tras un historial limpio).</summary>
        public async Task<bool> MarcarVerificadoAsync(int usuarioId, bool verificado, CancellationToken ct = default) =>
            await EjecutarAsync("UPDATE organizer_profile SET is_verified = @verificado WHERE user_id = @usuarioId",
                new { usuarioId, verificado }, ct) > 0;

        private static PerfilOrganizador Mapear(DbDataReader r) => new(
            r.Entero("user_id"),
            r.Texto("business_name"),
            r.TextoONulo("description"),
            r.Texto("social_url"),
            r.Texto("whatsapp"),
            r.Enum<EstadoOrganizador>("status"),
            r.Bool("is_verified"),
            r.Fecha("requested_at"),
            r.TextoONulo("rfc"));
    }
}
