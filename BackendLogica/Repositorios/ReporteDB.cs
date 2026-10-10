using System.Data.Common;
using BackendLogica.Configuracion;
using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace BackendLogica.Repositorios
{
    /// <summary>Reportes de usuarios sobre eventos, lugares o reseñas, y su resolución por moderadores.</summary>
    public sealed class ReporteDB : ConexionDB, IReportRepository
    {
        public ReporteDB(ConfiguracionBD? configuracion = null) : base(configuracion) { }

        /// <summary>Envía un reporte. Con motivo <see cref="MotivoReporte.Otro"/> los detalles son obligatorios.</summary>
        /// <returns>Id del reporte.</returns>
        public Task<int> CrearAsync(int reportanteId, TipoObjetivoReporte tipo, int objetivoId, MotivoReporte motivo, string? detalles, CancellationToken ct = default)
        {
            detalles = string.IsNullOrWhiteSpace(detalles) ? null : detalles.Trim();
            if (motivo == MotivoReporte.Otro && detalles == null)
                throw new DatosInvalidosException("Cuéntanos qué está pasando.");

            // Solo una de las cuatro columnas objetivo lleva valor (CHECK ck_rep_one_target).
            return InsertarAsync("""
                INSERT INTO report (reporter_id, event_id, place_id, event_review_id, place_review_id, reason, details)
                VALUES (@reportanteId, @evento, @lugar, @resenaEvento, @resenaLugar, @motivo, @detalles)
                """, new
            {
                reportanteId, motivo, detalles,
                evento = tipo == TipoObjetivoReporte.Evento ? objetivoId : (int?)null,
                lugar = tipo == TipoObjetivoReporte.Lugar ? objetivoId : (int?)null,
                resenaEvento = tipo == TipoObjetivoReporte.ResenaEvento ? objetivoId : (int?)null,
                resenaLugar = tipo == TipoObjetivoReporte.ResenaLugar ? objetivoId : (int?)null
            }, ct);
        }

        /// <summary>Bandeja de reportes (por defecto los abiertos, del más antiguo al más nuevo).</summary>
        public Task<List<Reporte>> ListarAsync(EstadoReporte estado = EstadoReporte.Abierto, CancellationToken ct = default) =>
            ConsultarAsync("SELECT * FROM report WHERE status = @estado ORDER BY created_at", Mapear, new { estado }, ct);

        /// <summary>Cierra un reporte como resuelto (se tomó acción) o descartado.</summary>
        public async Task<bool> ResolverAsync(int reporteId, int moderadorId, EstadoReporte resultado, CancellationToken ct = default)
        {
            if (resultado == EstadoReporte.Abierto)
                throw new DatosInvalidosException("Elige si el reporte se resolvió o se descarta.");
            return await EjecutarAsync("""
                UPDATE report SET status = @resultado, resolved_by = @moderadorId, resolved_at = NOW()
                WHERE id = @reporteId AND status = 'open'
                """, new { reporteId, moderadorId, resultado }, ct) > 0;
        }

        private static Reporte Mapear(DbDataReader r)
        {
            var (tipo, objetivo) =
                !r.EsNulo("event_id") ? (TipoObjetivoReporte.Evento, r.Entero("event_id")) :
                !r.EsNulo("place_id") ? (TipoObjetivoReporte.Lugar, r.Entero("place_id")) :
                !r.EsNulo("event_review_id") ? (TipoObjetivoReporte.ResenaEvento, r.Entero("event_review_id")) :
                (TipoObjetivoReporte.ResenaLugar, r.Entero("place_review_id"));

            return new Reporte(
                r.Entero("id"), r.Entero("reporter_id"), tipo, objetivo,
                r.Enum<MotivoReporte>("reason"), r.TextoONulo("details"), r.Enum<EstadoReporte>("status"),
                r.EnteroONulo("resolved_by"), r.FechaONula("resolved_at"), r.Fecha("created_at"));
        }
    }
}
