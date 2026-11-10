using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace Interfaz.Servicios
{
    /// <summary>
    /// Pantalla "Registrar lugar" (Paginas/RegistrarLugar.razor), que también sirve para editar, y la sección
    /// "Mis lugares" del Panel de organizador. Tablas <c>place</c>, <c>place_tag</c>, <c>place_hours</c> y
    /// <c>place_image</c>.
    /// </summary>
    /// <remarks>
    /// Reglas de la BD: solo organizadores aprobados registran lugares (<c>trg_place_bi</c>), el lugar nuevo queda
    /// pendiente, las coordenadas van dentro de Chihuahua y editar el contenido de uno aprobado (o agregarle fotos)
    /// lo regresa a moderación. Un lugar rechazado se corrige y se envía de nuevo con <see cref="ActualizarAsync"/>.
    /// </remarks>
    public sealed class ServicioRegistroLugar(ISuraChihBD bd, Sesion sesion)
    {
        /// <summary>Categorías de lugar y, al editar, el lugar con su galería.</summary>
        /// <param name="lugarId">null = lugar nuevo.</param>
        public async Task<DatosFormularioLugar> ObtenerFormularioAsync(int? lugarId)
        {
            Usuario dueno = await OrganizadorAsync();
            var etiquetas = bd.Etiquetas.ListarAsync();
            var detalle = lugarId is { } id ? bd.Lugares.ObtenerAsync(id) : Task.FromResult<LugarDetalle?>(null);
            await Task.WhenAll(etiquetas, detalle);

            RegistroModeracion? rechazo = null;
            if (lugarId != null)
            {
                LugarTarjeta l = detalle.Result?.Lugar is { } lugar && lugar.DuenoId == dueno.Id
                    ? lugar
                    : throw new ReglaNegocioException("Este lugar no existe o no es tuyo.");
                if (l.Estado == EstadoLugar.Inactivo) throw new ReglaNegocioException("Este lugar está dado de baja: ya no se puede editar.");
                if (l.Estado == EstadoLugar.Rechazado)
                    rechazo = await bd.Moderacion.ObtenerUltimaDecisionAsync(TipoObjetivoModeracion.Lugar, l.Id);
            }

            return new DatosFormularioLugar(
                [.. etiquetas.Result.Where(t => t.Alcance != AlcanceEtiqueta.Evento).OrderBy(t => t.Orden)],
                detalle.Result,
                rechazo);
        }

        /// <summary>"Enviar a revisión" de un lugar nuevo (la BD lo deja pendiente).</summary>
        /// <returns>Id del lugar.</returns>
        public async Task<int> CrearAsync(DatosLugar datos)
        {
            Usuario dueno = await OrganizadorAsync();
            return await bd.Lugares.CrearAsync(dueno.Id, datos);
        }

        /// <summary>
        /// Guarda los cambios de un lugar propio. Si estaba rechazado, además lo manda de nuevo a moderación.
        /// </summary>
        /// <returns>El estado en que quedó (Pendiente si volvió a revisión).</returns>
        public async Task<EstadoLugar> ActualizarAsync(int lugarId, DatosLugar datos)
        {
            Usuario dueno = await OrganizadorAsync();
            if (!await bd.Lugares.ActualizarAsync(lugarId, dueno.Id, datos))
                throw new ReglaNegocioException("Este lugar ya no existe o no es tuyo.");

            LugarTarjeta lugar = (await bd.Lugares.ObtenerAsync(lugarId))?.Lugar
                ?? throw new ReglaNegocioException("Este lugar ya no existe.");
            if (lugar.Estado == EstadoLugar.Rechazado && await bd.Lugares.EnviarARevisionAsync(lugarId, dueno.Id))
                return EstadoLugar.Pendiente;
            return lugar.Estado;
        }

        /// <summary>"Mis lugares" del panel, con la nota del moderador de los rechazados.</summary>
        public async Task<List<FilaLugarPanel>> ListarMiosAsync()
        {
            Usuario dueno = sesion.Requerir("ver tus lugares");
            var lugares = await bd.Lugares.ListarDeDuenoAsync(dueno.Id);
            var filas = await Task.WhenAll(lugares.Select(async l => new FilaLugarPanel(l,
                l.Estado == EstadoLugar.Rechazado ? await bd.Moderacion.ObtenerUltimaDecisionAsync(TipoObjetivoModeracion.Lugar, l.Id) : null)));
            return [.. filas];
        }

        /// <summary>Solo los organizadores aprobados registran lugares (la BD también lo revisa con un trigger).</summary>
        private async Task<Usuario> OrganizadorAsync()
        {
            Usuario usuario = sesion.Requerir("registrar lugares");
            if (sesion.EsOrganizador || await bd.Organizadores.EsAprobadoAsync(usuario.Id)) return usuario;
            throw new ReglaNegocioException("Solo los organizadores aprobados pueden registrar lugares.");
        }
    }

    /// <param name="Etiquetas">Categorías de lugar (Café, Museo, Parque...).</param>
    /// <param name="Lugar">El lugar que se edita (null si es nuevo), con su galería.</param>
    /// <param name="Rechazo">Si lo rechazaron: el motivo del moderador, para corregirlo.</param>
    public sealed record DatosFormularioLugar(
        IReadOnlyList<Etiqueta> Etiquetas,
        LugarDetalle? Lugar,
        RegistroModeracion? Rechazo);

    /// <param name="Rechazo">Última decisión de moderación si está rechazado (la nota bajo la fila).</param>
    public sealed record FilaLugarPanel(LugarTarjeta Lugar, RegistroModeracion? Rechazo);
}
