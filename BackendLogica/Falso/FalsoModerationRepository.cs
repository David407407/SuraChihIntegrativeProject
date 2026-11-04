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
                        e.ActualizadoEn = Ahora;
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

        public Task<List<RegistroModeracion>> ListarHistorialAsync(int limite = 50, DecisionModeracion? decision = null, TipoObjetivoModeracion? tipo = null, CancellationToken ct = default) =>
            Hacer(a => a.Moderaciones.Where(m => Coincide(m, decision, tipo))
                .OrderByDescending(m => m.DecididoEn).ThenByDescending(m => m.Id)
                .Take(limite).Select(m => ADatos(a, m)).ToList(), ct);

        public Task<int> ContarHistorialAsync(DecisionModeracion? decision = null, TipoObjetivoModeracion? tipo = null, CancellationToken ct = default) =>
            Hacer(a => a.Moderaciones.Count(m => Coincide(m, decision, tipo)), ct);

        public Task<List<RegistroResena>> ListarHistorialResenasAsync(int moderadorId, int limite = 50, CancellationToken ct = default) =>
            Hacer(a => AccionesVisibles(a, moderadorId).OrderByDescending(h => h.DecididoEn).ThenByDescending(h => h.Id)
                .Take(limite).Select(h => ADatos(a, h)).ToList(), ct);

        public Task<int> ContarHistorialResenasAsync(int moderadorId, CancellationToken ct = default) =>
            Hacer(a => AccionesVisibles(a, moderadorId).Count(), ct);

        public Task<RegistroModeracion?> ObtenerUltimaDecisionAsync(TipoObjetivoModeracion tipo, int objetivoId, CancellationToken ct = default) =>
            Hacer(a => a.Moderaciones.Where(m => m.Tipo == tipo && m.ObjetivoId == objetivoId)
                .OrderByDescending(m => m.DecididoEn).ThenByDescending(m => m.Id)
                .Select(m => ADatos(a, m)).FirstOrDefault(), ct);

        public Task<List<EnvioPendiente>> ListarEnviosPendientesAsync(CancellationToken ct = default) =>
            Hacer(a => a.Eventos.Where(e => e.Estado == EstadoEvento.Pendiente)
                .Select(e => new EnvioPendiente(TipoObjetivoModeracion.Evento, e.Id, e.ActualizadoEn))
                .Concat(a.Lugares.Where(l => l.Estado == EstadoLugar.Pendiente)
                    .Select(l => new EnvioPendiente(TipoObjetivoModeracion.Lugar, l.Id, l.CreadoEn)))
                .ToList(), ct);

        public Task<bool> DestacarEventoAsync(int eventoId, int? orden, CancellationToken ct = default) =>
            Hacer(a =>
            {
                var e = a.Evento(eventoId);
                if (e?.Estado != EstadoEvento.Aprobado) return false;
                e.Destacado = orden.HasValue;
                e.OrdenDestacado = orden;
                return true;
            }, ct);

        private static bool Coincide(FilaModeracion m, DecisionModeracion? decision, TipoObjetivoModeracion? tipo) =>
            (decision == null || m.Decision == decision) && (tipo == null || m.Tipo == tipo);

        /// <summary>Un supermoderador ve todo el historial de reseñas; un moderador, solo lo suyo.</summary>
        private static IEnumerable<FilaAccionResena> AccionesVisibles(AlmacenFalso a, int moderadorId)
        {
            var usuario = a.Usuario(moderadorId);
            if (usuario is not { EsModerador: true, Activo: true })
                throw new ReglaNegocioException(ConexionDB.SoloModeradores);
            return a.AccionesResenas.Where(h => usuario.EsSuperModerador || h.ModeradorId == moderadorId);
        }

        private static RegistroModeracion ADatos(AlmacenFalso a, FilaModeracion m) =>
            new(m.Id, m.ModeradorId, a.Usuario(m.ModeradorId)!.NombreUsuario, m.Tipo, m.ObjetivoId, m.Decision, m.Comentario, m.DecididoEn, m.Tipo switch
            {
                TipoObjetivoModeracion.Evento => a.Evento(m.ObjetivoId)!.Titulo,
                TipoObjetivoModeracion.Lugar => a.Lugar(m.ObjetivoId)!.Nombre,
                _ => a.Organizador(m.ObjetivoId)!.NombreNegocio
            });

        private static RegistroResena ADatos(AlmacenFalso a, FilaAccionResena h)
        {
            var resena = a.Resenas.First(r => r.Tipo == h.Tipo && r.Id == h.ResenaId);
            string objetivo = h.Tipo == TipoResena.Evento ? a.Evento(resena.ObjetivoId)!.Titulo : a.Lugar(resena.ObjetivoId)!.Nombre;
            return new RegistroResena(h.Id, h.ModeradorId, a.Usuario(h.ModeradorId)!.NombreUsuario, h.Tipo, h.ResenaId, resena.ObjetivoId, objetivo,
                a.Usuario(resena.UsuarioId)!.NombreUsuario, resena.Calificacion, resena.Comentario, h.Accion, h.DecididoEn);
        }
    }
}
