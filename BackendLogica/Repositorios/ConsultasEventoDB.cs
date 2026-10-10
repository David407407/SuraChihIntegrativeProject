using System.Data.Common;
using BackendLogica.Configuracion;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace BackendLogica.Repositorios
{
    /// <summary>
    /// Clase intermedia para los repositorios que devuelven eventos (<see cref="EventoDB"/>,
    /// <see cref="FavoritoDB"/>, <see cref="InscripcionDB"/>). Comparte el SELECT de la vista
    /// <c>v_event_card</c>, el mapeo a <see cref="EventoTarjeta"/> y la carga de etiquetas.
    /// </summary>
    public abstract class ConsultasEventoDB : ConexionDB
    {
        /// <summary>Columnas de la tarjeta (alias <c>c</c>) + contador de interesados.</summary>
        protected const string SelectTarjeta = """
            SELECT c.*,
                   (SELECT COUNT(*) FROM favorite f WHERE f.event_id = c.id) AS interested
            FROM v_event_card c
            """;

        /// <summary>Publicado y todavía no termina: lo que puede ver cualquier visitante.</summary>
        protected const string Vigente = "c.status = 'approved' AND c.end_at > NOW()";

        protected ConsultasEventoDB(ConfiguracionBD? configuracion) : base(configuracion) { }

        /// <summary>Ejecuta una consulta que empieza con <see cref="SelectTarjeta"/> y agrega las etiquetas.</summary>
        protected async Task<List<EventoTarjeta>> ConsultarTarjetasAsync(string sql, object? parametros = null, CancellationToken ct = default)
        {
            var eventos = await ConsultarAsync(sql, MapearTarjeta, parametros, ct);
            return await AgregarEtiquetasAsync(eventos, ct);
        }

        protected static EventoTarjeta MapearTarjeta(DbDataReader r) => new()
        {
            Id = r.Entero("id"),
            Titulo = r.Texto("title"),
            Descripcion = r.Texto("description"),
            Inicio = r.Fecha("start_at"),
            Fin = r.Fecha("end_at"),
            PrecioMin = r.Decimal("price_min"),
            PrecioMax = r.Decimal("price_max"),
            EsGratis = r.Bool("is_free"),
            BoletosUrl = r.TextoONulo("ticket_url"),
            ImagenUrl = r.TextoONulo("main_image_url"),
            Estado = r.Enum<EstadoEvento>("status"),
            Terminado = r.Bool("is_finished"),
            Destacado = r.Bool("is_featured"),
            OrdenDestacado = r.EnteroONulo("featured_order"),
            LugarId = r.EnteroONulo("place_id"),
            LugarNombre = r.TextoONulo("place_name"),
            Direccion = r.TextoONulo("address"),
            Latitud = r.DecimalONulo("latitude"),
            Longitud = r.DecimalONulo("longitude"),
            OrganizadorId = r.Entero("organizer_id"),
            OrganizadorNombre = r.Texto("organizer_name"),
            OrganizadorWhatsApp = r.Texto("organizer_whatsapp"),
            OrganizadorVerificado = r.Bool("organizer_verified"),
            Interesados = r.Entero("interested")
        };

        /// <summary>Carga las etiquetas de todos los eventos en una sola consulta (evita N+1).</summary>
        private async Task<List<EventoTarjeta>> AgregarEtiquetasAsync(List<EventoTarjeta> eventos, CancellationToken ct)
        {
            if (eventos.Count == 0) return eventos;

            var filas = await ConsultarAsync("""
                SELECT et.event_id, t.*
                FROM event_tag et JOIN tag t ON t.id = et.tag_id
                WHERE et.event_id IN @ids AND t.is_active
                ORDER BY t.sort_order, t.name
                """,
                r => (EventoId: r.Entero("event_id"), Etiqueta: EtiquetaDB.Mapear(r)),
                new { ids = eventos.Select(e => e.Id) }, ct);

            var porEvento = filas.ToLookup(f => f.EventoId, f => f.Etiqueta);
            return eventos.Select(e => e with { Etiquetas = porEvento[e.Id].ToList() }).ToList();
        }
    }
}
