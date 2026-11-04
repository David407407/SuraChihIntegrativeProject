using BackendLogica.Configuracion;
using BackendLogica.Contratos;
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
    public sealed class ModeracionDB : ConexionDB, IModerationRepository
    {
        /// <summary>Decisión con quién la tomó y sobre qué. El filtro o el orden se agregan al final.</summary>
        private const string SelectHistorial = """
            SELECT m.*, u.username AS mod_name, COALESCE(e.title, p.name, o.business_name) AS objetivo_nombre
            FROM moderation m
            JOIN user u ON u.id = m.mod_id
            LEFT JOIN event e ON e.id = m.event_id
            LEFT JOIN place p ON p.id = m.place_id
            LEFT JOIN organizer_profile o ON o.user_id = m.organizer_id
            """;

        public const string SoloSuperHistorial = "Solo un supermoderador puede ver el historial de reseñas ocultas.";

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

        /// <summary>Historial de decisiones, las más recientes primero. Sin filtro (null) trae todas.</summary>
        public Task<List<RegistroModeracion>> ListarHistorialAsync(int limite = 50, DecisionModeracion? decision = null, TipoObjetivoModeracion? tipo = null, CancellationToken ct = default) =>
            ConsultarAsync($"""
                {SelectHistorial}
                WHERE {FiltroHistorial(tipo)}
                ORDER BY m.decided_at DESC, m.id DESC
                LIMIT @limite
                """, Mapear, new { limite, decision }, ct);

        /// <summary>Total del historial con esos filtros ("Mostrando 7 de 214").</summary>
        public async Task<int> ContarHistorialAsync(DecisionModeracion? decision = null, TipoObjetivoModeracion? tipo = null, CancellationToken ct = default) =>
            await EscalarAsync<int>($"SELECT COUNT(*) FROM moderation m WHERE {FiltroHistorial(tipo)}", new { decision }, ct);

        /// <summary>Historial de reseñas ocultadas o vueltas a mostrar, las más recientes primero.</summary>
        /// <exception cref="ReglaNegocioException">No es supermoderador.</exception>
        public async Task<List<RegistroResena>> ListarHistorialResenasAsync(int superModeradorId, int limite = 50, CancellationToken ct = default)
        {
            await ExigirSuperModeradorAsync(superModeradorId, SoloSuperHistorial, ct);
            return await ConsultarAsync("""
                SELECT h.id, h.mod_id, h.action, h.decided_at, h.event_review_id, h.place_review_id,
                       moderador.username AS mod_name, autor.username AS autor_nombre,
                       COALESCE(er.event_id, pr.place_id) AS objetivo_id,
                       COALESCE(e.title, p.name) AS objetivo_nombre,
                       COALESCE(er.rating, pr.rating) AS calificacion,
                       COALESCE(er.comment, pr.comment) AS comentario
                FROM review_moderation h
                JOIN user moderador ON moderador.id = h.mod_id
                LEFT JOIN event_review er ON er.id = h.event_review_id
                LEFT JOIN place_review pr ON pr.id = h.place_review_id
                LEFT JOIN event e ON e.id = er.event_id
                LEFT JOIN place p ON p.id = pr.place_id
                JOIN user autor ON autor.id = COALESCE(er.user_id, pr.user_id)
                ORDER BY h.decided_at DESC, h.id DESC
                LIMIT @limite
                """, r => new RegistroResena(
                    r.Entero("id"), r.Entero("mod_id"), r.Texto("mod_name"),
                    r.EsNulo("event_review_id") ? TipoResena.Lugar : TipoResena.Evento,
                    r.EnteroONulo("event_review_id") ?? r.Entero("place_review_id"),
                    r.Entero("objetivo_id"), r.Texto("objetivo_nombre"), r.Texto("autor_nombre"), r.Entero("calificacion"),
                    r.TextoONulo("comentario"), r.Enum<AccionResena>("action"), r.Fecha("decided_at")), new { limite }, ct);
        }

        /// <summary>Total del historial de reseñas.</summary>
        /// <exception cref="ReglaNegocioException">No es supermoderador.</exception>
        public async Task<int> ContarHistorialResenasAsync(int superModeradorId, CancellationToken ct = default)
        {
            await ExigirSuperModeradorAsync(superModeradorId, SoloSuperHistorial, ct);
            return await EscalarAsync<int>("SELECT COUNT(*) FROM review_moderation", ct: ct);
        }

        /// <summary>"Ver motivo" del panel del organizador: la decisión más reciente sobre ese objetivo.</summary>
        public Task<RegistroModeracion?> ObtenerUltimaDecisionAsync(TipoObjetivoModeracion tipo, int objetivoId, CancellationToken ct = default)
        {
            return ConsultarUnoAsync($"""
                {SelectHistorial}
                WHERE m.{Columna(tipo)} = @objetivoId
                ORDER BY m.decided_at DESC, m.id DESC
                LIMIT 1
                """, Mapear, new { objetivoId }, ct);
        }

        /// <summary>Fecha de envío de lo pendiente: la última modificación del evento o lugar.</summary>
        public Task<List<EnvioPendiente>> ListarEnviosPendientesAsync(CancellationToken ct = default) =>
            ConsultarAsync("""
                SELECT 'event' AS tipo, id, updated_at FROM event WHERE status = 'pending'
                UNION ALL
                SELECT 'place', id, updated_at FROM place WHERE status = 'pending'
                """, r => new EnvioPendiente(
                    r.Texto("tipo") == "event" ? TipoObjetivoModeracion.Evento : TipoObjetivoModeracion.Lugar,
                    r.Entero("id"), r.Fecha("updated_at")), ct: ct);

        /// <summary>
        /// Pone (o quita, con <paramref name="orden"/> null) un evento aprobado en el carrusel del hero.
        /// </summary>
        public async Task<bool> DestacarEventoAsync(int eventoId, int? orden, CancellationToken ct = default) =>
            await EjecutarAsync("""
                UPDATE event SET is_featured = @destacado, featured_order = @orden
                WHERE id = @eventoId AND status = 'approved'
                """, new { eventoId, orden, destacado = orden.HasValue }, ct) > 0;

        /// <summary>Nombre de columna constante según el tipo: nunca viene del usuario.</summary>
        private static string Columna(TipoObjetivoModeracion tipo) => tipo switch
        {
            TipoObjetivoModeracion.Evento => "event_id",
            TipoObjetivoModeracion.Lugar => "place_id",
            _ => "organizer_id"
        };

        /// <summary>Condición para @decision (null = todas) y el tipo elegido.</summary>
        private static string FiltroHistorial(TipoObjetivoModeracion? tipo)
        {
            string filtro = "(@decision IS NULL OR m.decision = @decision)";
            return tipo is { } t ? $"{filtro} AND m.{Columna(t)} IS NOT NULL" : filtro;
        }

        private static RegistroModeracion Mapear(System.Data.Common.DbDataReader r)
        {
            var (tipo, objetivo) =
                !r.EsNulo("event_id") ? (TipoObjetivoModeracion.Evento, r.Entero("event_id")) :
                !r.EsNulo("place_id") ? (TipoObjetivoModeracion.Lugar, r.Entero("place_id")) :
                (TipoObjetivoModeracion.Organizador, r.Entero("organizer_id"));
            return new RegistroModeracion(
                r.Entero("id"), r.Entero("mod_id"), r.Texto("mod_name"), tipo, objetivo,
                r.Enum<DecisionModeracion>("decision"), r.TextoONulo("comment"), r.Fecha("decided_at"), r.Texto("objetivo_nombre"));
        }
    }
}
