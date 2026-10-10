using BackendLogica.Contratos;
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

        /// <summary>Con Evento se incluyen las de alcance Evento y Ambos (igual para Lugar).</summary>
        private static IEnumerable<Etiqueta> Filtrar(AlmacenFalso a, AlcanceEtiqueta? para) =>
            a.Etiquetas.Where(t => t.Activa).Select(t => t.Datos)
                       .Where(t => para is null or AlcanceEtiqueta.Ambos || t.Alcance == para || t.Alcance == AlcanceEtiqueta.Ambos);
    }
}
