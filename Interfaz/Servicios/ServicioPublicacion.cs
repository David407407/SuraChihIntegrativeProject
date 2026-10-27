using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace Interfaz.Servicios
{
    /// <summary>
    /// Pantalla "Publicar evento" (Paginas/PublicarEvento.razor, WBS 4.6), que también sirve para editar, y sus
    /// modales "Enviado a revisión" y "Editar evento publicado". Tablas <c>event</c>, <c>event_tag</c> y
    /// <c>event_image</c> (en una sola transacción, ver <see cref="IEventRepository.CrearAsync"/>).
    /// </summary>
    /// <remarks>
    /// Las reglas las cuida la BD: solo organizadores aprobados publican, fin posterior al inicio, coordenadas
    /// dentro de Chihuahua, precio mínimo ≤ máximo y título de hasta 150 caracteres. La pantalla revisa lo mismo
    /// antes para marcar el campo con error. Editar un evento aprobado lo regresa a revisión y lo quita del carrusel.
    /// </remarks>
    public sealed class ServicioPublicacion(ISuraChihBD bd, Sesion sesion)
    {
        /// <summary>Lo que necesita el formulario: categorías, lugares para "Lugar registrado" y, al editar, el evento.</summary>
        /// <param name="eventoId">null = evento nuevo.</param>
        public async Task<DatosFormularioEvento> ObtenerFormularioAsync(int? eventoId)
        {
            Usuario organizador = await OrganizadorAsync();
            var etiquetas = bd.Etiquetas.ListarAsync();
            var lugares = bd.Lugares.ListarAprobadosAsync();
            var detalle = eventoId is { } id ? bd.Eventos.ObtenerAsync(id) : Task.FromResult<EventoDetalle?>(null);
            await Task.WhenAll(etiquetas, lugares, detalle);

            int inscritos = 0;
            if (eventoId != null)
            {
                EventoTarjeta e = detalle.Result?.Evento is { } evento && evento.OrganizadorId == organizador.Id
                    ? evento
                    : throw new ReglaNegocioException("Este evento no existe o no es tuyo.");
                if (e.Estado == EstadoEvento.Cancelado) throw new ReglaNegocioException("Este evento está cancelado: ya no se puede editar.");
                if (e.Terminado) throw new ReglaNegocioException("Este evento ya terminó: ya no se puede editar.");
                if (e.Estado == EstadoEvento.Aprobado)
                    inscritos = (await bd.Inscripciones.ListarInscritosAsync(e.Id, organizador.Id)).Count;
            }

            return new DatosFormularioEvento(
                [.. etiquetas.Result.Where(t => t.Alcance != AlcanceEtiqueta.Lugar).OrderBy(t => t.Orden)],
                [.. lugares.Result.OrderBy(l => l.Nombre)],
                detalle.Result,
                inscritos);
        }

        /// <summary>"Enviar a revisión" o "Guardar borrador" de un evento nuevo.</summary>
        /// <returns>Id del evento.</returns>
        public async Task<int> CrearAsync(DatosEvento datos, bool enviarARevision)
        {
            Usuario organizador = await OrganizadorAsync();
            return await bd.Eventos.CrearAsync(organizador.Id, datos, enviarARevision);
        }

        /// <summary>
        /// Guarda los cambios de un evento propio. Con <paramref name="enviarARevision"/>, un borrador o un
        /// evento rechazado se manda además a moderación.
        /// </summary>
        public async Task<ResultadoEdicionEvento> ActualizarAsync(int eventoId, DatosEvento datos, bool enviarARevision)
        {
            Usuario organizador = await OrganizadorAsync();
            ResultadoEdicionEvento resultado = await bd.Eventos.ActualizarAsync(eventoId, organizador.Id, datos)
                ?? throw new ReglaNegocioException("Este evento ya no existe, no es tuyo o está cancelado.");

            if (enviarARevision && resultado.Estado is EstadoEvento.Borrador or EstadoEvento.Rechazado)
            {
                if (!await bd.Eventos.EnviarARevisionAsync(eventoId, organizador.Id))
                    throw new ReglaNegocioException("No se pudo enviar a revisión. Intenta de nuevo.");
                resultado = resultado with { Estado = EstadoEvento.Pendiente };
            }
            return resultado;
        }

        /// <summary>Solo los organizadores aprobados publican (la BD también lo revisa con un trigger).</summary>
        private async Task<Usuario> OrganizadorAsync()
        {
            Usuario usuario = sesion.Requerir("publicar eventos");
            if (sesion.EsOrganizador || await bd.Organizadores.EsAprobadoAsync(usuario.Id)) return usuario;
            throw new ReglaNegocioException("Solo los organizadores aprobados pueden publicar eventos.");
        }
    }

    /// <param name="Etiquetas">Categorías de evento ("Puedes elegir varias").</param>
    /// <param name="Lugares">Lugares aprobados para "Lugar registrado".</param>
    /// <param name="Evento">El evento que se edita (null si es nuevo), con su galería.</param>
    /// <param name="Inscritos">"Las N inscripciones se conservan" del modal "Editar evento publicado".</param>
    public sealed record DatosFormularioEvento(
        IReadOnlyList<Etiqueta> Etiquetas,
        IReadOnlyList<LugarTarjeta> Lugares,
        EventoDetalle? Evento,
        int Inscritos);
}
