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

                var fila = new FilaReporte
                {
                    Id = AlmacenFalso.Siguiente(a.Reportes, r => r.Id), ReportanteId = reportanteId, Tipo = tipo,
                    ObjetivoId = objetivoId, Motivo = motivo, Detalles = detalles, Estado = EstadoReporte.Abierto, CreadoEn = Ahora
                };
                a.Reportes.Add(fila);
                return fila.Id;
            }, ct);

        public Task<List<Reporte>> ListarAsync(EstadoReporte estado = EstadoReporte.Abierto, CancellationToken ct = default) =>
            Hacer(a => a.Reportes.Where(r => r.Estado == estado).OrderBy(r => r.CreadoEn).Select(r => r.ADatos()).ToList(), ct);

        public Task<bool> ResolverAsync(int reporteId, int moderadorId, EstadoReporte resultado, CancellationToken ct = default) =>
            Hacer(a =>
            {
                if (resultado == EstadoReporte.Abierto)
                    throw new DatosInvalidosException("Elige si el reporte se resolvió o se descarta.");
                var fila = a.Reportes.FirstOrDefault(r => r.Id == reporteId && r.Estado == EstadoReporte.Abierto);
                if (fila == null) return false;
                fila.Estado = resultado;
                fila.ResueltoPor = moderadorId;
                fila.ResueltoEn = Ahora;
                return true;
            }, ct);
    }
}
