using BackendLogica.Configuracion;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace BackendLogica.Repositorios
{
    /// <summary>
    /// Decisiones de moderación. Insertar una decisión actualiza automáticamente el estado del
    /// evento, lugar o solicitud (trigger <c>trg_moderation_ai</c>), y la BD valida que quien decide
    /// sea moderador, que el objetivo esté pendiente y que no sea suyo.
    /// </summary>
    /// <remarks>
    /// Para obtener lo pendiente usa <see cref="EventoDB.ListarPorEstadoAsync"/>,
    /// <see cref="LugarDB.ListarPorEstadoAsync"/> y <see cref="OrganizadorDB.ListarPorEstadoAsync"/>.
    /// </remarks>
    public sealed class ModeracionDB : ConexionDB
    {
        public ModeracionDB(ConfiguracionBD? configuracion = null) : base(configuracion) { }

        /// <summary>Aprueba o rechaza. Rechazar exige <paramref name="comentario"/> (se le muestra al autor).</summary>
        /// <returns>Id del registro de moderación.</returns>
        /// <exception cref="ReglaNegocioException">No es moderador, el objetivo no está pendiente o es suyo.</exception>
        public Task<int> DecidirAsync(int moderadorId, TipoObjetivoModeracion tipo, int objetivoId,
            DecisionModeracion decision, string? comentario = null, CancellationToken ct = default)
        {
            comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
            if (decision == DecisionModeracion.Rechazado && comentario == null)
                throw new DatosInvalidosException("Escribe el motivo del rechazo.");

            return InsertarAsync("""
                INSERT INTO moderation (mod_id, event_id, place_id, organizer_id, decision, comment)
                VALUES (@moderadorId, @evento, @lugar, @organizador, @decision, @comentario)
                """, new
            {
                moderadorId, decision, comentario,
                evento = tipo == TipoObjetivoModeracion.Evento ? objetivoId : (int?)null,
                lugar = tipo == TipoObjetivoModeracion.Lugar ? objetivoId : (int?)null,
                organizador = tipo == TipoObjetivoModeracion.Organizador ? objetivoId : (int?)null
            }, ct);
        }

        public Task<int> AprobarAsync(int moderadorId, TipoObjetivoModeracion tipo, int objetivoId, string? comentario = null, CancellationToken ct = default) =>
            DecidirAsync(moderadorId, tipo, objetivoId, DecisionModeracion.Aprobado, comentario, ct);

        public Task<int> RechazarAsync(int moderadorId, TipoObjetivoModeracion tipo, int objetivoId, string motivo, CancellationToken ct = default) =>
            DecidirAsync(moderadorId, tipo, objetivoId, DecisionModeracion.Rechazado, motivo, ct);

        /// <summary>Historial de decisiones, las más recientes primero.</summary>
        public Task<List<RegistroModeracion>> ListarHistorialAsync(int limite = 50, CancellationToken ct = default) =>
            ConsultarAsync("""
                SELECT m.*, u.username AS mod_name
                FROM moderation m JOIN user u ON u.id = m.mod_id
                ORDER BY m.decided_at DESC, m.id DESC
                LIMIT @limite
                """, r =>
            {
                var (tipo, objetivo) =
                    !r.EsNulo("event_id") ? (TipoObjetivoModeracion.Evento, r.Entero("event_id")) :
                    !r.EsNulo("place_id") ? (TipoObjetivoModeracion.Lugar, r.Entero("place_id")) :
                    (TipoObjetivoModeracion.Organizador, r.Entero("organizer_id"));
                return new RegistroModeracion(
                    r.Entero("id"), r.Entero("mod_id"), r.Texto("mod_name"), tipo, objetivo,
                    r.Enum<DecisionModeracion>("decision"), r.TextoONulo("comment"), r.Fecha("decided_at"));
            }, new { limite }, ct);

        /// <summary>
        /// Pone (o quita, con <paramref name="orden"/> null) un evento aprobado en el carrusel del hero.
        /// </summary>
        public async Task<bool> DestacarEventoAsync(int eventoId, int? orden, CancellationToken ct = default) =>
            await EjecutarAsync("""
                UPDATE event SET is_featured = @destacado, featured_order = @orden
                WHERE id = @eventoId AND status = 'approved'
                """, new { eventoId, orden, destacado = orden.HasValue }, ct) > 0;
    }
}
