using BackendLogica.Configuracion;
using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace BackendLogica.Repositorios
{
    /// <summary>
    /// Inscripciones ("Voy a ir"). No reservan cupo: los boletos se compran en la página del evento.
    /// </summary>
    public sealed class InscripcionDB : ConsultasEventoDB, IInscriptionRepository
    {
        public InscripcionDB(ConfiguracionBD? configuracion = null) : base(configuracion) { }

        /// <summary>
        /// Inscribe al usuario. Si había cancelado antes, reactiva la inscripción.
        /// La BD rechaza eventos no aprobados o terminados con <see cref="ReglaNegocioException"/>.
        /// </summary>
        public Task<int> InscribirAsync(int usuarioId, int eventoId, CancellationToken ct = default) =>
            EjecutarAsync("""
                INSERT INTO inscription (user_id, event_id) VALUES (@usuarioId, @eventoId)
                ON DUPLICATE KEY UPDATE status = 'active', cancelled_at = NULL, created_at = NOW()
                """, new { usuarioId, eventoId }, ct);

        /// <returns>false si no estaba inscrito.</returns>
        public async Task<bool> CancelarAsync(int usuarioId, int eventoId, CancellationToken ct = default) =>
            await EjecutarAsync("""
                UPDATE inscription SET status = 'cancelled', cancelled_at = NOW()
                WHERE user_id = @usuarioId AND event_id = @eventoId AND status = 'active'
                """, new { usuarioId, eventoId }, ct) > 0;

        public async Task<bool> EstaInscritoAsync(int usuarioId, int eventoId, CancellationToken ct = default) =>
            await EscalarAsync<int>("""
                SELECT COUNT(*) FROM inscription
                WHERE user_id = @usuarioId AND event_id = @eventoId AND status = 'active'
                """, new { usuarioId, eventoId }, ct) > 0;

        /// <summary>Pantalla "Mis planes" (próximos, pasados o cancelados).</summary>
        public async Task<List<PlanUsuario>> ListarPlanesAsync(int usuarioId, PestanaPlanes pestana, CancellationToken ct = default)
        {
            string filtro = pestana switch
            {
                PestanaPlanes.Proximos => "i.status = 'active' AND c.end_at > NOW() AND c.status = 'approved'",
                PestanaPlanes.Pasados => "i.status = 'active' AND c.end_at <= NOW()",
                _ => "(i.status = 'cancelled' OR c.status = 'cancelled')"
            };
            string orden = pestana == PestanaPlanes.Proximos ? "c.start_at" : "c.start_at DESC";

            // 1) Inscripciones de la pestaña, ya ordenadas. 2) Sus tarjetas completas (con etiquetas).
            var filas = await ConsultarAsync($"""
                SELECT i.event_id, i.status, i.created_at, i.cancelled_at
                FROM inscription i JOIN v_event_card c ON c.id = i.event_id
                WHERE i.user_id = @usuarioId AND {filtro}
                ORDER BY {orden}
                """, r => (Id: r.Entero("event_id"),
                           Estado: r.Enum<EstadoInscripcion>("status"),
                           Creada: r.Fecha("created_at"),
                           Cancelada: r.FechaONula("cancelled_at")),
                new { usuarioId }, ct);

            if (filas.Count == 0) return [];
            var eventos = (await ConsultarTarjetasAsync($"{SelectTarjeta} WHERE c.id IN @ids", new { ids = filas.Select(f => f.Id) }, ct))
                .ToDictionary(e => e.Id);

            return filas.Where(f => eventos.ContainsKey(f.Id))
                        .Select(f => new PlanUsuario(eventos[f.Id], f.Estado, f.Creada, f.Cancelada))
                        .ToList();
        }

        /// <summary>Tabla "Inscritos" de las estadísticas. Vacía si el evento no es de este organizador.</summary>
        public Task<List<Inscrito>> ListarInscritosAsync(int eventoId, int organizadorId, CancellationToken ct = default) =>
            ConsultarAsync("""
                SELECT i.user_id, u.username, u.avatar_url, i.status, i.created_at, i.cancelled_at
                FROM inscription i
                JOIN event e ON e.id = i.event_id AND e.organizer_id = @organizadorId
                JOIN user u ON u.id = i.user_id
                WHERE i.event_id = @eventoId
                ORDER BY i.created_at, i.user_id
                """, r => new Inscrito(
                    r.Entero("user_id"), r.Texto("username"), r.TextoONulo("avatar_url"),
                    r.Enum<EstadoInscripcion>("status"), r.Fecha("created_at"), r.FechaONula("cancelled_at")),
                new { eventoId, organizadorId }, ct);
    }
}
