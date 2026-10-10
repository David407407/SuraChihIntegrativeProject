using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace BackendLogica.Falso
{
    /// <summary>
    /// <see cref="IModerationRepository"/> en memoria. Repite <c>trg_moderation_bi</c> (validaciones) y
    /// <c>trg_moderation_ai</c> (aplicar la decisión al evento, lugar o solicitud).
    /// </summary>
    public sealed class FalsoModerationRepository : RepositorioFalso, IModerationRepository
    {
        internal FalsoModerationRepository(AlmacenFalso almacen) : base(almacen) { }

        public Task<int> DecidirAsync(int moderadorId, TipoObjetivoModeracion tipo, int objetivoId,
            DecisionModeracion decision, string? comentario = null, CancellationToken ct = default) =>
            Hacer(a =>
            {
                comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
                if (decision == DecisionModeracion.Rechazado && comentario == null)
                    throw new DatosInvalidosException("Escribe el motivo del rechazo.");

                // trg_moderation_bi
                if (a.Usuario(moderadorId) is not { EsModerador: true, Activo: true }) throw Trigger(MensajesTrigger.SoloModeradores);
                switch (tipo)
                {
                    case TipoObjetivoModeracion.Evento:
                        var e = a.Evento(objetivoId);
                        if (e?.Estado != EstadoEvento.Pendiente) throw Trigger(MensajesTrigger.EventoNoPendiente);
                        if (e.OrganizadorId == moderadorId) throw Trigger(MensajesTrigger.PropioEvento);
                        e.Estado = decision == DecisionModeracion.Aprobado ? EstadoEvento.Aprobado : EstadoEvento.Rechazado;
                        break;
                    case TipoObjetivoModeracion.Lugar:
                        var l = a.Lugar(objetivoId);
                        if (l?.Estado != EstadoLugar.Pendiente) throw Trigger(MensajesTrigger.LugarNoPendiente);
                        if (l.DuenoId == moderadorId) throw Trigger(MensajesTrigger.PropioLugar);
                        l.Estado = decision == DecisionModeracion.Aprobado ? EstadoLugar.Aprobado : EstadoLugar.Rechazado;
                        break;
                    default:
                        var o = a.Organizador(objetivoId);
                        if (o?.Estado != EstadoOrganizador.Pendiente) throw Trigger(MensajesTrigger.SolicitudNoPendiente);
                        if (objetivoId == moderadorId) throw Trigger(MensajesTrigger.PropiaSolicitud);
                        o.Estado = decision == DecisionModeracion.Aprobado ? EstadoOrganizador.Aprobado : EstadoOrganizador.Rechazado;
                        break;
                }

                var fila = new FilaModeracion(AlmacenFalso.Siguiente(a.Moderaciones, m => m.Id), moderadorId, tipo, objetivoId,
                    decision, comentario, Ahora);
                a.Moderaciones.Add(fila);
                return fila.Id;
            }, ct);

        public Task<int> AprobarAsync(int moderadorId, TipoObjetivoModeracion tipo, int objetivoId, string? comentario = null, CancellationToken ct = default) =>
            DecidirAsync(moderadorId, tipo, objetivoId, DecisionModeracion.Aprobado, comentario, ct);

        public Task<int> RechazarAsync(int moderadorId, TipoObjetivoModeracion tipo, int objetivoId, string motivo, CancellationToken ct = default) =>
            DecidirAsync(moderadorId, tipo, objetivoId, DecisionModeracion.Rechazado, motivo, ct);

        public Task<List<RegistroModeracion>> ListarHistorialAsync(int limite = 50, CancellationToken ct = default) =>
            Hacer(a => a.Moderaciones.OrderByDescending(m => m.DecididoEn).ThenByDescending(m => m.Id)
                .Take(limite).Select(m => ADatos(a, m)).ToList(), ct);

        public Task<RegistroModeracion?> ObtenerUltimaDecisionAsync(TipoObjetivoModeracion tipo, int objetivoId, CancellationToken ct = default) =>
            Hacer(a => a.Moderaciones.Where(m => m.Tipo == tipo && m.ObjetivoId == objetivoId)
                .OrderByDescending(m => m.DecididoEn).ThenByDescending(m => m.Id)
                .Select(m => ADatos(a, m)).FirstOrDefault(), ct);

        public Task<bool> DestacarEventoAsync(int eventoId, int? orden, CancellationToken ct = default) =>
            Hacer(a =>
            {
                var e = a.Evento(eventoId);
                if (e?.Estado != EstadoEvento.Aprobado) return false;
                e.Destacado = orden.HasValue;
                e.OrdenDestacado = orden;
                return true;
            }, ct);

        private static RegistroModeracion ADatos(AlmacenFalso a, FilaModeracion m) =>
            new(m.Id, m.ModeradorId, a.Usuario(m.ModeradorId)!.NombreUsuario, m.Tipo, m.ObjetivoId, m.Decision, m.Comentario, m.DecididoEn);
    }
}
