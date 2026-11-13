using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace BackendLogica.Falso
{
    /// <summary><see cref="ITagRepository"/> en memoria.</summary>
    public sealed class FalsoTagRepository : RepositorioFalso, ITagRepository
    {
        internal FalsoTagRepository(AlmacenFalso almacen) : base(almacen) { }

        public Task<List<Etiqueta>> ListarAsync(AlcanceEtiqueta? para = null, CancellationToken ct = default) =>
            Hacer(a => Filtrar(a, para).OrderBy(t => t.Orden).ThenBy(t => t.Nombre).ToList(), ct);

        public Task<int> ContarAsync(AlcanceEtiqueta? para = null, CancellationToken ct = default) =>
            Hacer(a => Filtrar(a, para).Count(), ct);

        public Task<Gustos> ObtenerGustosAsync(int usuarioId, CancellationToken ct = default) =>
            Hacer(a => new Gustos(
                a.Gustos.Where(g => g.UsuarioId == usuarioId && a.Etiquetas.Any(t => t.Activa && t.Datos.Id == g.EtiquetaId))
                        .Select(g => g.EtiquetaId).ToHashSet(),
                a.Usuario(usuarioId)?.AvisosGustos ?? false), ct);

        public Task GuardarGustosAsync(int usuarioId, IReadOnlyCollection<int> etiquetaIds, bool avisos, CancellationToken ct = default) =>
            Hacer(a =>
            {
                // FK fk_interest_user
                var usuario = a.Usuario(usuarioId) ?? throw new ReglaNegocioException("La cuenta no existe.");
                a.Gustos.RemoveAll(g => g.UsuarioId == usuarioId);
                a.Gustos.AddRange(a.Etiquetas.Where(t => t.Activa && etiquetaIds.Contains(t.Datos.Id))
                                             .Select(t => new FilaGusto(usuarioId, t.Datos.Id)));
                usuario.AvisosGustos = avisos;
            }, ct);

        /// <summary>Con Evento se incluyen las de alcance Evento y Ambos (igual para Lugar).</summary>
        private static IEnumerable<Etiqueta> Filtrar(AlmacenFalso a, AlcanceEtiqueta? para) =>
            a.Etiquetas.Where(t => t.Activa).Select(t => t.Datos)
                       .Where(t => para is null or AlcanceEtiqueta.Ambos || t.Alcance == para || t.Alcance == AlcanceEtiqueta.Ambos);
    }
}
