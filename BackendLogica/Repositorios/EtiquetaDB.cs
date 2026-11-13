using System.Data.Common;
using BackendLogica.Configuracion;
using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace BackendLogica.Repositorios
{
    /// <summary>Etiquetas / categorías (chips de filtro).</summary>
    public sealed class EtiquetaDB : ConexionDB, ITagRepository
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

        public async Task<Gustos> ObtenerGustosAsync(int usuarioId, CancellationToken ct = default)
        {
            var ids = await ConsultarAsync("""
                SELECT ui.tag_id FROM user_interest ui
                JOIN tag t ON t.id = ui.tag_id AND t.is_active
                WHERE ui.user_id = @usuarioId
                """, r => r.Entero("tag_id"), new { usuarioId }, ct);
            bool avisos = await EscalarAsync<bool>("SELECT notify_interests FROM user WHERE id = @usuarioId", new { usuarioId }, ct);
            return new Gustos(ids.ToHashSet(), avisos);
        }

        public Task GuardarGustosAsync(int usuarioId, IReadOnlyCollection<int> etiquetaIds, bool avisos, CancellationToken ct = default) =>
            EnTransaccionAsync(async tx =>
            {
                await tx.EjecutarAsync("DELETE FROM user_interest WHERE user_id = @usuarioId", new { usuarioId });
                if (etiquetaIds.Count > 0)
                    await tx.EjecutarAsync("""
                        INSERT INTO user_interest (user_id, tag_id)
                        SELECT @usuarioId, id FROM tag WHERE is_active AND id IN @etiquetas
                        """, new { usuarioId, etiquetas = etiquetaIds.Distinct().ToArray() });
                await tx.EjecutarAsync("UPDATE user SET notify_interests = @avisos WHERE id = @usuarioId", new { usuarioId, avisos });
                return true;
            }, ct);

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
