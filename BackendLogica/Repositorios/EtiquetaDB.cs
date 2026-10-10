using System.Data.Common;
using BackendLogica.Configuracion;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace BackendLogica.Repositorios
{
    /// <summary>Etiquetas / categorías (chips de filtro).</summary>
    public sealed class EtiquetaDB : ConexionDB
    {
        public EtiquetaDB(ConfiguracionBD? configuracion = null) : base(configuracion) { }

        /// <summary>
        /// Etiquetas activas en el orden del frontend. Con <paramref name="para"/> = Evento se incluyen
        /// las de alcance Evento y Ambos (igual para Lugar).
        /// </summary>
        public Task<List<Etiqueta>> ListarAsync(AlcanceEtiqueta? para = null, CancellationToken ct = default) =>
            ConsultarAsync($"""
                SELECT * FROM tag
                WHERE is_active {FiltroAlcance(para)}
                ORDER BY sort_order, name
                """, Mapear, new { alcance = para }, ct);

        /// <summary>Cuántas categorías activas hay (dato del hero de la landing).</summary>
        public async Task<int> ContarAsync(AlcanceEtiqueta? para = null, CancellationToken ct = default) =>
            await EscalarAsync<int>($"SELECT COUNT(*) FROM tag WHERE is_active {FiltroAlcance(para)}", new { alcance = para }, ct);

        private static string FiltroAlcance(AlcanceEtiqueta? para) =>
            para is null or AlcanceEtiqueta.Ambos ? "" : "AND scope IN (@alcance, 'both')";

        internal static Etiqueta Mapear(DbDataReader r) => new(
            r.Entero("id"),
            r.Texto("name"),
            r.Texto("icon"),
            r.Texto("color_hex"),
            r.Enum<AlcanceEtiqueta>("scope"),
            r.Entero("sort_order"));
    }
}
