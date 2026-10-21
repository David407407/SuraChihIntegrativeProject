using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace Interfaz.Servicios
{
    /// <summary>
    /// Datos de la hoja "Evento / ..." de Figma (Componentes/Detalle/DetalleEvento.razor) y de los
    /// modales "Confirmar inscripción" / "Cancelar inscripción" (Componentes/Modales/ModalInscripcion.razor).
    /// </summary>
    /// <remarks>
    /// Los 24 frames "Evento / ..." son la misma plantilla con datos distintos. El estado (normal, "Ya vas a
    /// ir", finalizado o cancelado) lo decide la interfaz con <c>Estado</c>, <c>Terminado</c> y
    /// <c>MotivoCancelacion</c> de <see cref="EventoTarjeta"/> (vista <c>v_event_card</c>) y con
    /// <see cref="DatosDetalleEvento.Inscrito"/>.
    /// </remarks>
    public sealed class ServicioEvento(ISuraChihBD bd, Sesion sesion)
    {
        /// <summary>Tarjetas de "Te podría interesar" (una fila de tres en Figma).</summary>
        private const int Recomendados = 3;

        public async Task<DatosDetalleEvento> ObtenerAsync(int eventoId)
        {
            EventoDetalle detalle = await bd.Eventos.ObtenerAsync(eventoId)
                ?? throw new ReglaNegocioException("Este evento ya no existe.");
            EventoTarjeta evento = detalle.Evento;
            Usuario? usuario = sesion.Usuario;

            // Publicados y cancelados los ve cualquiera (los inscritos deben enterarse de la cancelación).
            // Borradores, pendientes y rechazados solo su organizador o un moderador.
            bool publico = evento.Estado is EstadoEvento.Aprobado or EstadoEvento.Cancelado;
            bool puedeVerlo = publico || (usuario != null && (usuario.EsModerador || usuario.Id == evento.OrganizadorId));
            if (!puedeVerlo) throw new ReglaNegocioException("Este evento no está disponible.");

            var recomendados = ListarRecomendadosAsync(evento);
            var favoritos = usuario != null ? bd.Favoritos.ListarIdsAsync(usuario.Id) : Task.FromResult(new HashSet<int>());
            var inscrito = usuario != null ? bd.Inscripciones.EstaInscritoAsync(usuario.Id, eventoId) : Task.FromResult(false);
            // "Ver reseñas (12)" solo sale cuando el evento ya terminó.
            Task<ResumenResenas>? resenas = evento.Terminado ? bd.Resenas.ObtenerResumenAsync(TipoResena.Evento, eventoId) : null;
            await Task.WhenAll(recomendados, favoritos, inscrito, resenas ?? Task.CompletedTask);

            if (evento.Estado == EstadoEvento.Aprobado) await RegistrarVistaAsync(eventoId, usuario?.Id);

            return new DatosDetalleEvento(
                evento,
                detalle.Galeria,
                inscrito.Result,
                favoritos.Result.Contains(eventoId),
                resenas?.Result.Total,
                recomendados.Result,
                favoritos.Result);
        }

        /// <summary>
        /// "Te podría interesar": eventos vigentes con alguna de sus categorías y, si no alcanzan,
        /// los más guardados. Nunca el mismo evento.
        /// </summary>
        private async Task<List<EventoTarjeta>> ListarRecomendadosAsync(EventoTarjeta evento)
        {
            var etiquetas = evento.Etiquetas.Select(t => t.Id).ToList();
            var parecidos = etiquetas.Count > 0
                ? bd.Eventos.FiltrarAsync(new FiltroEventos { EtiquetaIds = etiquetas, Limite = Recomendados + 1 })
                : Task.FromResult(new List<EventoTarjeta>());
            var populares = bd.Eventos.ListarMasGuardadosAsync(limite: Recomendados + 1);
            await Task.WhenAll(parecidos, populares);

            return parecidos.Result.Concat(populares.Result)
                .Where(e => e.Id != evento.Id)
                .DistinctBy(e => e.Id)
                .Take(Recomendados)
                .ToList();
        }

        /// <summary>El embudo del organizador no debe tumbar el detalle si falla.</summary>
        private async Task RegistrarVistaAsync(int eventoId, int? usuarioId)
        {
            try { await bd.Eventos.RegistrarVistaAsync(eventoId, usuarioId); }
            catch (SuraChihException ex) { System.Diagnostics.Debug.WriteLine($"[Detalle] vista no registrada: {ex.Message}"); }
        }

        // ---------------------------------------------------------------- "Voy a ir" / "Cancelar inscripción"

        /// <returns>true: el usuario quedó inscrito ("Ya vas a ir").</returns>
        public async Task<bool> InscribirAsync(int eventoId)
        {
            Usuario usuario = sesion.Requerir("apartar tu lugar");
            // El trigger trg_inscription_bi rechaza eventos no aprobados o terminados con su propio mensaje.
            await bd.Inscripciones.InscribirAsync(usuario.Id, eventoId);
            return true;
        }

        /// <returns>false: el usuario ya no está inscrito.</returns>
        public async Task<bool> CancelarInscripcionAsync(int eventoId)
        {
            Usuario usuario = sesion.Requerir("cancelar tu inscripción");
            await bd.Inscripciones.CancelarAsync(usuario.Id, eventoId);
            return false;
        }
    }

    /// <param name="Evento">Fila de <c>v_event_card</c>: trae <c>Estado</c>, <c>Terminado</c> y <c>MotivoCancelacion</c>.</param>
    /// <param name="Galeria">Fotos extra del evento (la portada es <c>Evento.ImagenUrl</c>).</param>
    /// <param name="Inscrito">El usuario ya dijo "Voy a ir" (botón verde "Ya vas a ir"). Siempre false sin sesión.</param>
    /// <param name="Favorito">Corazón lleno en la portada.</param>
    /// <param name="NumResenas">Para "Ver reseñas (N)". Solo viene cuando el evento ya terminó.</param>
    /// <param name="Recomendados">"Te podría interesar" (hasta 3).</param>
    /// <param name="Favoritos">Ids de eventos favoritos del usuario, para los corazones de las recomendaciones.</param>
    public sealed record DatosDetalleEvento(
        EventoTarjeta Evento,
        IReadOnlyList<string> Galeria,
        bool Inscrito,
        bool Favorito,
        int? NumResenas,
        IReadOnlyList<EventoTarjeta> Recomendados,
        IReadOnlySet<int> Favoritos);
}
