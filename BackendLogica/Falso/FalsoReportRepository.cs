using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace BackendLogica.Falso
{
    /// <summary><see cref="IReportRepository"/> en memoria.</summary>
    public sealed class FalsoReportRepository : RepositorioFalso, IReportRepository
    {
        internal FalsoReportRepository(AlmacenFalso almacen) : base(almacen) { }

        public Task<int> CrearAsync(int reportanteId, TipoObjetivoReporte tipo, int objetivoId, MotivoReporte motivo, string? detalles, CancellationToken ct = default) =>
            Hacer(a =>
            {
                detalles = string.IsNullOrWhiteSpace(detalles) ? null : detalles.Trim();
                if (motivo == MotivoReporte.Otro && detalles == null)
                    throw new DatosInvalidosException("Cuéntanos qué está pasando.");

                bool existe = tipo switch
                {
                    TipoObjetivoReporte.Evento => a.Evento(objetivoId) != null,
                    TipoObjetivoReporte.Lugar => a.Lugar(objetivoId) != null,
                    TipoObjetivoReporte.ResenaEvento => a.Resenas.Any(r => r.Tipo == TipoResena.Evento && r.Id == objetivoId),
                    _ => a.Resenas.Any(r => r.Tipo == TipoResena.Lugar && r.Id == objetivoId)
                };
                if (!existe || a.Usuario(reportanteId) == null) throw AlmacenFalso.NoExiste();

                // trg_report_bi
                switch (tipo)
                {
                    case TipoObjetivoReporte.Evento when a.Evento(objetivoId)!.OrganizadorId == reportanteId:
                        throw Trigger(MensajesTrigger.ReportePropioEvento);
                    case TipoObjetivoReporte.Lugar when a.Lugar(objetivoId)!.DuenoId == reportanteId:
                        throw Trigger(MensajesTrigger.ReportePropioLugar);
                    case TipoObjetivoReporte.ResenaEvento when a.Resenas.Any(r => r.Tipo == TipoResena.Evento && r.Id == objetivoId && r.UsuarioId == reportanteId):
                    case TipoObjetivoReporte.ResenaLugar when a.Resenas.Any(r => r.Tipo == TipoResena.Lugar && r.Id == objetivoId && r.UsuarioId == reportanteId):
                        throw Trigger(MensajesTrigger.ReportePropiaResena);
                }

                // uq_rep_once
                if (a.Reportes.Any(r => r.ReportanteId == reportanteId && r.Tipo == tipo && r.ObjetivoId == objetivoId))
                    throw new DuplicadoException("Ya reportaste esto.");

                var fila = new FilaReporte
                {
                    Id = AlmacenFalso.Siguiente(a.Reportes, r => r.Id), ReportanteId = reportanteId, Tipo = tipo,
                    ObjetivoId = objetivoId, Motivo = motivo, Detalles = detalles, Estado = EstadoReporte.Abierto, CreadoEn = Ahora
                };
                a.Reportes.Add(fila);
                return fila.Id;
            }, ct);

        public Task<bool> YaReportoAsync(int reportanteId, TipoObjetivoReporte tipo, int objetivoId, CancellationToken ct = default) =>
            Hacer(a => a.Reportes.Any(r => r.ReportanteId == reportanteId && r.Tipo == tipo && r.ObjetivoId == objetivoId), ct);

        public Task<List<Reporte>> ListarAsync(EstadoReporte estado = EstadoReporte.Abierto, CancellationToken ct = default) =>
            Hacer(a => a.Reportes.Where(r => r.Estado == estado).OrderBy(r => r.CreadoEn).Select(r => r.ADatos()).ToList(), ct);

        public Task<List<ReporteDetalle>> ListarDetalleAsync(EstadoReporte estado = EstadoReporte.Abierto, CancellationToken ct = default) =>
            Hacer(a =>
            {
                var filas = a.Reportes.Where(r => r.Estado == estado);
                filas = estado == EstadoReporte.Abierto
                    ? filas.OrderBy(r => r.CreadoEn).ThenBy(r => r.Id)
                    : filas.OrderByDescending(r => r.ResueltoEn).ThenByDescending(r => r.Id);
                return filas.Select(r => ADetalle(a, r)).ToList();
            }, ct);

        public Task<bool> ResolverAsync(int reporteId, int moderadorId, EstadoReporte resultado, CancellationToken ct = default) =>
            Hacer(a =>
            {
                if (resultado == EstadoReporte.Abierto)
                    throw new DatosInvalidosException("Elige si el reporte se resolvió o se descarta.");
                if (a.Usuario(moderadorId) is not { EsModerador: true, Activo: true })
                    throw new ReglaNegocioException(ConexionDB.SoloModeradores);
                var fila = a.Reportes.FirstOrDefault(r => r.Id == reporteId && r.Estado == EstadoReporte.Abierto);
                if (fila == null) return false;
                fila.Estado = resultado;
                fila.ResueltoPor = moderadorId;
                fila.ResueltoEn = Ahora;
                return true;
            }, ct);

        public Task<bool> ReabrirAsync(int reporteId, int moderadorId, CancellationToken ct = default) =>
            Hacer(a =>
            {
                if (a.Usuario(moderadorId) is not { EsModerador: true, Activo: true })
                    throw new ReglaNegocioException(ConexionDB.SoloModeradores);
                var fila = a.Reportes.FirstOrDefault(r => r.Id == reporteId && r.Estado != EstadoReporte.Abierto);
                if (fila == null) return false;
                fila.Estado = EstadoReporte.Abierto;
                fila.ResueltoPor = null;
                fila.ResueltoEn = null;
                return true;
            }, ct);

        private static ReporteDetalle ADetalle(AlmacenFalso a, FilaReporte r)
        {
            string reportante = a.Usuario(r.ReportanteId)!.NombreUsuario;
            switch (r.Tipo)
            {
                case TipoObjetivoReporte.Evento:
                    return new ReporteDetalle(r.ADatos(), reportante, r.ObjetivoId, null, a.Evento(r.ObjetivoId)!.Titulo);
                case TipoObjetivoReporte.Lugar:
                    return new ReporteDetalle(r.ADatos(), reportante, null, r.ObjetivoId, a.Lugar(r.ObjetivoId)!.Nombre);
            }

            bool deEvento = r.Tipo == TipoObjetivoReporte.ResenaEvento;
            var resena = a.Resenas.First(f => f.Tipo == (deEvento ? TipoResena.Evento : TipoResena.Lugar) && f.Id == r.ObjetivoId);
            string objetivo = deEvento ? a.Evento(resena.ObjetivoId)!.Titulo : a.Lugar(resena.ObjetivoId)!.Nombre;
            return new ReporteDetalle(r.ADatos(), reportante, deEvento ? resena.ObjetivoId : null, deEvento ? null : resena.ObjetivoId, objetivo,
                a.Usuario(resena.UsuarioId)!.NombreUsuario, resena.Calificacion, resena.Comentario, resena.Oculta);
        }
    }
}
