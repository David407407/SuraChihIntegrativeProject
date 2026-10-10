using BackendLogica.Contratos;
using BackendLogica.Modelos;

namespace BackendLogica.Falso
{
    /// <summary><see cref="IInscriptionRepository"/> en memoria, con la regla del trigger <c>trg_inscription_bi</c>.</summary>
    public sealed class FalsoInscriptionRepository : RepositorioFalso, IInscriptionRepository
    {
        internal FalsoInscriptionRepository(AlmacenFalso almacen) : base(almacen) { }

        /// <returns>1 si es nueva, 2 si se reactivó una anterior (como INSERT ... ON DUPLICATE KEY UPDATE).</returns>
        public Task<int> InscribirAsync(int usuarioId, int eventoId, CancellationToken ct = default) =>
            Hacer(a =>
            {
                var e = a.Evento(eventoId);
                if (e == null || !AlmacenFalso.Vigente(e, Ahora)) throw Trigger(MensajesTrigger.NoPuedesInscribirte);
                if (a.Usuario(usuarioId) == null) throw AlmacenFalso.NoExiste();

                var fila = Buscar(a, usuarioId, eventoId);
                if (fila == null)
                {
                    a.Inscripciones.Add(new FilaInscripcion { UsuarioId = usuarioId, EventoId = eventoId, Estado = EstadoInscripcion.Activa, CreadaEn = Ahora });
                    return 1;
                }
                fila.Estado = EstadoInscripcion.Activa;
                fila.CanceladaEn = null;
                fila.CreadaEn = Ahora;
                return 2;
            }, ct);

        public Task<bool> CancelarAsync(int usuarioId, int eventoId, CancellationToken ct = default) =>
            Hacer(a =>
            {
                var fila = Buscar(a, usuarioId, eventoId);
                if (fila is not { Estado: EstadoInscripcion.Activa }) return false;
                fila.Estado = EstadoInscripcion.Cancelada;
                fila.CanceladaEn = Ahora;
                return true;
            }, ct);

        public Task<bool> EstaInscritoAsync(int usuarioId, int eventoId, CancellationToken ct = default) =>
            Hacer(a => Buscar(a, usuarioId, eventoId) is { Estado: EstadoInscripcion.Activa }, ct);

        public Task<List<PlanUsuario>> ListarPlanesAsync(int usuarioId, PestanaPlanes pestana, CancellationToken ct = default) =>
            Hacer(a =>
            {
                DateTime ahora = Ahora;
                var filas = a.Inscripciones.Where(i => i.UsuarioId == usuarioId)
                    .Select(i => (Inscripcion: i, Evento: a.Evento(i.EventoId)!))
                    .Where(x => pestana switch
                    {
                        PestanaPlanes.Proximos => x.Inscripcion.Estado == EstadoInscripcion.Activa && AlmacenFalso.Vigente(x.Evento, ahora),
                        PestanaPlanes.Pasados => x.Inscripcion.Estado == EstadoInscripcion.Activa && !x.Evento.FechaPorConfirmar && x.Evento.Fin <= ahora,
                        _ => x.Inscripcion.Estado == EstadoInscripcion.Cancelada || x.Evento.Estado == EstadoEvento.Cancelado
                    });
                filas = pestana == PestanaPlanes.Proximos
                    ? filas.OrderBy(x => AlmacenFalso.OrdenFecha(x.Evento))
                    : filas.OrderByDescending(x => x.Evento.Inicio);
                return filas.Select(x => new PlanUsuario(a.Tarjeta(x.Evento, ahora), x.Inscripcion.Estado,
                    x.Inscripcion.CreadaEn, x.Inscripcion.CanceladaEn)).ToList();
            }, ct);

        public Task<List<Inscrito>> ListarInscritosAsync(int eventoId, int organizadorId, CancellationToken ct = default) =>
            Hacer(a =>
            {
                if (a.Evento(eventoId)?.OrganizadorId != organizadorId) return [];
                return a.Inscripciones.Where(i => i.EventoId == eventoId)
                    .OrderBy(i => i.CreadaEn).ThenBy(i => i.UsuarioId)
                    .Select(i =>
                    {
                        var u = a.Usuario(i.UsuarioId)!;
                        return new Inscrito(u.Id, u.NombreUsuario, u.AvatarUrl, i.Estado, i.CreadaEn, i.CanceladaEn);
                    }).ToList();
            }, ct);

        private static FilaInscripcion? Buscar(AlmacenFalso a, int usuarioId, int eventoId) =>
            a.Inscripciones.FirstOrDefault(i => i.UsuarioId == usuarioId && i.EventoId == eventoId);
    }
}
