using BackendLogica.Modelos;

namespace BackendLogica.Contratos
{
    /// <summary>
    /// Categorías: chips de filtro, "Explora por categoría" y el selector de "Publicar evento".
    /// Implementaciones: <c>EtiquetaDB</c> y <c>FalsoTagRepository</c>.
    /// </summary>
    public interface ITagRepository
    {
        /// <summary>
        /// Etiquetas activas en el orden del frontend. Con <paramref name="para"/> = Evento se incluyen
        /// las de alcance Evento y Ambos (igual para Lugar).
        /// </summary>
        Task<List<Etiqueta>> ListarAsync(AlcanceEtiqueta? para = null, CancellationToken ct = default);

        /// <summary>Cuántas categorías activas hay (dato del hero de la landing).</summary>
        Task<int> ContarAsync(AlcanceEtiqueta? para = null, CancellationToken ct = default);

        /// <summary>Gustos del usuario (sus categorías y si acepta avisos). Sin gustos, la lista va vacía.</summary>
        Task<Gustos> ObtenerGustosAsync(int usuarioId, CancellationToken ct = default);

        /// <summary>
        /// Reemplaza los gustos del usuario por <paramref name="etiquetaIds"/> (las inactivas o inexistentes se
        /// ignoran) y guarda si acepta avisos.
        /// </summary>
        Task GuardarGustosAsync(int usuarioId, IReadOnlyCollection<int> etiquetaIds, bool avisos, CancellationToken ct = default);
    }
}
