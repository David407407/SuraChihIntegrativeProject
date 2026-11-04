using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace Interfaz.Servicios
{
    /// <summary>
    /// Pantallas "Organizador / Panel" (Paginas/PanelOrganizador.razor) y "Organizador / Estadísticas del evento"
    /// (Paginas/EstadisticasEvento.razor), con el modal "Cancelar evento". Datos: <c>v_event_card</c>,
    /// <c>v_event_stats</c>, <c>event_view</c> e <c>inscription</c>.
    /// </summary>
    /// <remarks>
    /// Un organizador solo ve y cancela lo suyo: se revisa aquí y otra vez en BackendLogica, que filtra por
    /// <c>organizer_id</c>. Las cifras son las que hay en la BD; ninguna se inventa.
    /// </remarks>
    public sealed class ServicioPanelOrganizador(ISuraChihBD bd, Sesion sesion)
    {
        /// <summary>"Últimos 30 días, todos tus eventos" de la métrica Vistas.</summary>
        private const int DiasVistas = 30;
        /// <summary>La gráfica muestra 7 días; se piden 14 para comparar contra los 7 anteriores ("+18%").</summary>
        private const int DiasGrafica = 7;

        /// <summary>El panel, o null si el usuario no es organizador aprobado (la pantalla lo manda a "Hazte organizador").</summary>
        public async Task<DatosPanel?> ObtenerPanelAsync()
        {
            Usuario usuario = sesion.Requerir("entrar a tu panel de organizador");
            PerfilOrganizador? perfil = await bd.Organizadores.ObtenerAsync(usuario.Id);
            bool aprobado = perfil?.Estado == EstadoOrganizador.Aprobado;
            if (aprobado != sesion.EsOrganizador) sesion.Iniciar(usuario, aprobado);
            if (!aprobado) return null;

            var eventos = bd.Eventos.ListarDeOrganizadorAsync(usuario.Id);
            var estadisticas = bd.Eventos.ListarEstadisticasAsync(usuario.Id);
            var envios = bd.Moderacion.ListarEnviosPendientesAsync();
            await Task.WhenAll(eventos, estadisticas, envios);

            var porEvento = estadisticas.Result.ToDictionary(s => s.EventoId);
            var enviadoEn = envios.Result.Where(x => x.Tipo == TipoObjetivoModeracion.Evento).ToDictionary(x => x.ObjetivoId, x => x.EnviadoEn);

            var filas = await Task.WhenAll(eventos.Result.Select(async e => new FilaPanel(
                e,
                porEvento.GetValueOrDefault(e.Id),
                enviadoEn.TryGetValue(e.Id, out var enviado) ? enviado : null,
                e.Estado == EstadoEvento.Rechazado ? await bd.Moderacion.ObtenerUltimaDecisionAsync(TipoObjetivoModeracion.Evento, e.Id) : null)));

            // Solo lo que llegó a publicarse suma en vistas recientes; en borrador o revisión no hay vistas.
            var vistas = await Task.WhenAll(eventos.Result.Select(e => bd.Eventos.ListarVistasPorDiaAsync(e.Id, DiasVistas)));

            var totales = estadisticas.Result;
            int resenas = totales.Sum(s => s.NumResenas);
            decimal? calificacion = resenas == 0 ? null
                : Math.Round(totales.Sum(s => (s.Calificacion ?? 0) * s.NumResenas) / resenas, 1, MidpointRounding.AwayFromZero);

            return new DatosPanel(
                perfil!, usuario.NombreUsuario, filas,
                vistas.Sum(dias => dias.Sum(d => d.Vistas)),
                totales.Sum(s => s.Interesados),
                totales.Sum(s => s.Inscritos),
                calificacion,
                resenas);
        }

        /// <summary>Encabezado, embudo, gráfica e inscritos de un evento propio.</summary>
        /// <exception cref="ReglaNegocioException">El evento no existe o es de otro organizador.</exception>
        public async Task<DatosEstadisticas> ObtenerEstadisticasAsync(int eventoId)
        {
            Usuario usuario = sesion.Requerir("ver las estadísticas de tu evento");
            EventoTarjeta evento = (await bd.Eventos.ObtenerAsync(eventoId))?.Evento is { } e && e.OrganizadorId == usuario.Id
                ? e
                : throw new ReglaNegocioException("Este evento no existe o no es tuyo.");

            var estadisticas = bd.Eventos.ListarEstadisticasAsync(usuario.Id);
            var dias = bd.Eventos.ListarVistasPorDiaAsync(eventoId, DiasGrafica * 2);
            var inscritos = bd.Inscripciones.ListarInscritosAsync(eventoId, usuario.Id);
            await Task.WhenAll(estadisticas, dias, inscritos);

            var ordenados = dias.Result.OrderBy(d => d.Dia).ToList();
            return new DatosEstadisticas(
                evento,
                estadisticas.Result.FirstOrDefault(s => s.EventoId == eventoId) ?? new EstadisticasEvento(eventoId, 0, 0, 0, null, 0),
                [.. ordenados.TakeLast(DiasGrafica)],
                CambioPorcentual(ordenados.TakeLast(DiasGrafica).Sum(d => d.Vistas), ordenados.SkipLast(DiasGrafica).Sum(d => d.Vistas)),
                inscritos.Result);
        }

        /// <summary>"Cancelar evento": queda cancelado con su motivo y los inscritos lo ven así en Mis planes.</summary>
        public async Task CancelarAsync(int eventoId, string motivo)
        {
            Usuario usuario = sesion.Requerir("cancelar tu evento");
            if (!await bd.Eventos.CancelarAsync(eventoId, usuario.Id, motivo))
                throw new ReglaNegocioException("Este evento ya está cancelado o no es tuyo.");
        }

        /// <summary>"+18%" contra los días anteriores; null si antes no hubo vistas (no hay contra qué comparar).</summary>
        private static int? CambioPorcentual(int actual, int anterior) =>
            anterior == 0 ? null : (int)Math.Round((actual - anterior) * 100m / anterior, MidpointRounding.AwayFromZero);
    }

    /// <param name="Filas">Tabla "Mis eventos", del más reciente al más antiguo.</param>
    /// <param name="Vistas">Vistas de todos sus eventos en los últimos 30 días.</param>
    /// <param name="Interesados">Corazones de todos sus eventos.</param>
    /// <param name="Inscritos">Inscripciones activas de todos sus eventos.</param>
    /// <param name="Calificacion">Promedio ponderado por reseñas; null si no tiene.</param>
    public sealed record DatosPanel(
        PerfilOrganizador Perfil,
        string NombreUsuario,
        IReadOnlyList<FilaPanel> Filas,
        int Vistas,
        int Interesados,
        int Inscritos,
        decimal? Calificacion,
        int NumResenas);

    /// <summary>Un renglón de "Mis eventos".</summary>
    /// <param name="Estadisticas">Fila de <c>v_event_stats</c>.</param>
    /// <param name="EnviadoEn">"Enviado 8 oct" de los que están en revisión.</param>
    /// <param name="Rechazo">La decisión de moderación de los rechazados ("Nota del moderador").</param>
    public sealed record FilaPanel(EventoTarjeta Evento, EstadisticasEvento? Estadisticas, DateTime? EnviadoEn, RegistroModeracion? Rechazo);

    /// <param name="Dias">Los últimos 7 días, de más antiguo a hoy, incluidos los días en 0.</param>
    /// <param name="Cambio">Vistas de estos 7 días contra los 7 anteriores, en %; null si antes no hubo.</param>
    /// <param name="Inscritos">Activos y cancelados, en orden de inscripción.</param>
    public sealed record DatosEstadisticas(
        EventoTarjeta Evento,
        EstadisticasEvento Estadisticas,
        IReadOnlyList<VistasDia> Dias,
        int? Cambio,
        IReadOnlyList<Inscrito> Inscritos);
}
