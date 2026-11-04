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

        /// <summary>"Moderación / Reportes": cada reporte con su evento o lugar, los más nuevos primero.</summary>
        public Task<List<ReporteDetalle>> ListarDetalleAsync(EstadoReporte estado = EstadoReporte.Abierto, CancellationToken ct = default) =>
            ConsultarAsync("""
                SELECT r.*, reportante.username AS reportante_nombre,
                       COALESCE(r.event_id, er.event_id) AS evento_vinculado,
                       COALESCE(r.place_id, pr.place_id) AS lugar_vinculado,
                       COALESCE(e.title, p.name) AS objetivo_nombre,
                       autor.username AS resena_autor,
                       COALESCE(er.rating, pr.rating) AS resena_calificacion,
                       COALESCE(er.comment, pr.comment) AS resena_comentario,
                       COALESCE(er.is_hidden, pr.is_hidden, FALSE) AS resena_oculta
                FROM report r
                JOIN user reportante ON reportante.id = r.reporter_id
                LEFT JOIN event_review er ON er.id = r.event_review_id
                LEFT JOIN place_review pr ON pr.id = r.place_review_id
                LEFT JOIN event e ON e.id = COALESCE(r.event_id, er.event_id)
                LEFT JOIN place p ON p.id = COALESCE(r.place_id, pr.place_id)
                LEFT JOIN user autor ON autor.id = COALESCE(er.user_id, pr.user_id)
                WHERE r.status = @estado
                ORDER BY r.created_at DESC, r.id DESC
                """, l => new ReporteDetalle(
                    Mapear(l), l.Texto("reportante_nombre"), l.EnteroONulo("evento_vinculado"), l.EnteroONulo("lugar_vinculado"),
                    l.Texto("objetivo_nombre"), l.TextoONulo("resena_autor"), l.EnteroONulo("resena_calificacion"),
                    l.TextoONulo("resena_comentario"), l.Bool("resena_oculta")), new { estado }, ct);

        /// <summary>Cierra un reporte como resuelto (se tomó acción) o descartado.</summary>
        /// <exception cref="ReglaNegocioException">No es moderador.</exception>
        public async Task<bool> ResolverAsync(int reporteId, int moderadorId, EstadoReporte resultado, CancellationToken ct = default)
        {
            if (resultado == EstadoReporte.Abierto)
                throw new DatosInvalidosException("Elige si el reporte se resolvió o se descarta.");
            await ExigirModeradorAsync(moderadorId, ct);
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
