using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;
using BackendLogica.Repositorios;
using BackendLogica.Seguridad;

namespace BackendLogica.Falso
{
    /// <summary>
    /// <see cref="IReviewRepository"/> en memoria, con las reglas de <c>trg_evrev_bi</c>, <c>trg_plrev_bi</c>
    /// y las llaves <c>uq_evrev_once</c> / <c>uq_plrev_once</c> (una reseña por usuario).
    /// </summary>
    public sealed class FalsoReviewRepository : RepositorioFalso, IReviewRepository
    {
        internal FalsoReviewRepository(AlmacenFalso almacen) : base(almacen) { }

        public Task<int> CrearAsync(TipoResena tipo, int objetivoId, int usuarioId, int calificacion, string? comentario, CancellationToken ct = default) =>
            Hacer(a =>
            {
                Validar.Calificacion(calificacion);
                if (tipo == TipoResena.Evento)
                {
                    var e = a.Evento(objetivoId);
                    if (e is not { Estado: EstadoEvento.Aprobado, FechaPorConfirmar: false } || e.Fin >= Ahora)
                        throw Trigger(MensajesTrigger.ResenaEventoNoTerminado);
                    if (e.OrganizadorId == usuarioId) throw Trigger(MensajesTrigger.ResenaPropioEvento);
                }
                else
                {
                    var l = a.Lugar(objetivoId);
                    if (l is not { Estado: EstadoLugar.Aprobado }) throw Trigger(MensajesTrigger.ResenaLugarNoDisponible);
                    if (l.DuenoId == usuarioId) throw Trigger(MensajesTrigger.ResenaPropioLugar);
                }
                if (a.Usuario(usuarioId) == null) throw AlmacenFalso.NoExiste();
                if (De(a, tipo).Any(r => r.ObjetivoId == objetivoId && r.UsuarioId == usuarioId))
                    throw new DuplicadoException("Ya dejaste una reseña aquí.");

                var fila = new FilaResena
                {
                    Id = AlmacenFalso.Siguiente(De(a, tipo), r => r.Id), Tipo = tipo, ObjetivoId = objetivoId,
                    UsuarioId = usuarioId, Calificacion = calificacion, Comentario = Limpiar(comentario), CreadaEn = Ahora
                };
                a.Resenas.Add(fila);
                return fila.Id;
            }, ct);

        public Task<bool> EditarAsync(TipoResena tipo, int resenaId, int usuarioId, int calificacion, string? comentario, CancellationToken ct = default) =>
            Hacer(a =>
            {
                Validar.Calificacion(calificacion);
                var fila = De(a, tipo).FirstOrDefault(r => r.Id == resenaId && r.UsuarioId == usuarioId);
                if (fila == null) return false;
                fila.Calificacion = calificacion;
                fila.Comentario = Limpiar(comentario);
                return true;
            }, ct);

        public Task<bool> ResponderAsync(TipoResena tipo, int resenaId, int duenoId, string respuesta, CancellationToken ct = default) =>
            Hacer(a =>
            {
                Validar.Requerido(respuesta, "La respuesta");
                var fila = De(a, tipo).FirstOrDefault(r => r.Id == resenaId);
                int? dueno = tipo == TipoResena.Evento ? a.Evento(fila?.ObjetivoId ?? 0)?.OrganizadorId : a.Lugar(fila?.ObjetivoId)?.DuenoId;
                if (fila == null || dueno != duenoId) return false;
                fila.Respuesta = respuesta.Trim();
                fila.RespondidaEn = Ahora;
                return true;
            }, ct);

        public Task<List<Resena>> ListarAsync(TipoResena tipo, int objetivoId, bool incluirOcultas = false, CancellationToken ct = default) =>
            Hacer(a => De(a, tipo)
                .Where(r => r.ObjetivoId == objetivoId && (incluirOcultas || !r.Oculta))
                .OrderByDescending(r => r.CreadaEn).ThenByDescending(r => r.Id)
                .Select(r =>
                {
                    var u = a.Usuario(r.UsuarioId)!;
                    return new Resena(r.Id, tipo, r.ObjetivoId, r.UsuarioId, u.NombreUsuario, u.AvatarUrl, r.Calificacion,
                        r.Comentario, r.Respuesta, r.RespondidaEn, r.Oculta, r.CreadaEn);
                }).ToList(), ct);

        public Task<ResumenResenas> ObtenerResumenAsync(TipoResena tipo, int objetivoId, CancellationToken ct = default) =>
            Hacer(a => ResenaDB.Resumir(De(a, tipo)
                .Where(r => r.ObjetivoId == objetivoId && !r.Oculta)
                .GroupBy(r => r.Calificacion).Select(g => (g.Key, g.Count()))), ct);

        public Task<bool> CambiarVisibilidadAsync(TipoResena tipo, int resenaId, bool oculta, CancellationToken ct = default) =>
            Hacer(a =>
            {
                var fila = De(a, tipo).FirstOrDefault(r => r.Id == resenaId);
                if (fila == null) return false;
                fila.Oculta = oculta;
                return true;
            }, ct);

        /// <summary>event_review o place_review.</summary>
        private static IEnumerable<FilaResena> De(AlmacenFalso a, TipoResena tipo) => a.Resenas.Where(r => r.Tipo == tipo);

        private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    }
}
