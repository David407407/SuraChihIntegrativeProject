using BackendLogica.Contratos;
using BackendLogica.Datos;
using BackendLogica.Modelos;

namespace Interfaz.Servicios
{
    /// <summary>
    /// Pantallas "Moderación / Publicaciones" (Paginas/Moderacion.razor) y "Moderación / Revisar evento"
    /// (Paginas/RevisarEvento.razor), con sus modales "Aprobar", "Rechazar" y "Decisión registrada".
    /// Tablas <c>moderation</c> y <c>event</c> (<c>status</c>, <c>is_featured</c>, <c>featured_order</c>).
    /// </summary>
    /// <remarks>
    /// Las reglas las cuida la BD (trigger <c>trg_moderation_bi</c>): solo moderadores, el objetivo debe
    /// estar pendiente y nadie modera lo suyo. Al insertar la decisión, <c>trg_moderation_ai</c> cambia el
    /// estado del evento. La "Lista de revisión" de Figma no se guarda (no tiene tabla): es solo una ayuda.
    /// </remarks>
    public sealed class ServicioModeracion(ISuraChihBD bd, Sesion sesion)
    {
        /// <summary>Decisiones que se leen para contar las de hoy.</summary>
        private const int HistorialHoy = 200;

        /// <summary>Motivos del modal "Rechazar". "Otro" no se antepone al comentario.</summary>
        public static readonly string[] MotivosRechazo =
        [
            "Información incompleta o falsa",
            "Ubicación fuera de Chihuahua",
            "Imágenes inapropiadas o sin permiso",
            "No se pudo verificar que sea real",
            "Otro",
        ];

        public const string MotivoOtro = "Otro";

        // ---------------------------------------------------------------- Bandeja

        /// <summary>Eventos y lugares pendientes (los más antiguos primero) y las cifras de arriba.</summary>
        public async Task<DatosBandeja> ObtenerBandejaAsync()
        {
            Usuario moderador = Moderador();
            var eventos = bd.Eventos.ListarPorEstadoAsync(EstadoEvento.Pendiente);
            var lugares = bd.Lugares.ListarPorEstadoAsync(EstadoLugar.Pendiente);
            var envios = bd.Moderacion.ListarEnviosPendientesAsync();
            var organizadores = bd.Organizadores.ListarPorEstadoAsync(EstadoOrganizador.Pendiente);
            var reportes = bd.Reportes.ListarAsync(EstadoReporte.Abierto);
            var historial = bd.Moderacion.ListarHistorialAsync(HistorialHoy);
            await Task.WhenAll(eventos, lugares, envios, organizadores, reportes, historial);

            var enviadoEn = envios.Result.ToDictionary(e => (e.Tipo, e.ObjetivoId), e => e.EnviadoEn);
            DateTime Enviado(TipoObjetivoModeracion tipo, int id) => enviadoEn.GetValueOrDefault((tipo, id), DateTime.Now);

            // "Primer evento": su organizador todavía no tiene eventos aprobados.
            var conAprobados = await Task.WhenAll(eventos.Result.Select(e => e.OrganizadorId).Distinct()
                .Select(async id => (id, aprobados: (await bd.Eventos.ListarDeOrganizadorAsync(id)).Any(Publicado))));
            var nombresLugares = await Task.WhenAll(lugares.Result.Select(l => l.DuenoId).Distinct()
                .Select(async id => (id, nombre: (await bd.Organizadores.ObtenerAsync(id))?.NombreNegocio)));

            var pendientesEventos = eventos.Result.Select(async e => new PendienteModeracion(
                TipoObjetivoModeracion.Evento, e, null, e.OrganizadorNombre,
                Enviado(TipoObjetivoModeracion.Evento, e.Id),
                Nuevo: !conAprobados.Single(o => o.id == e.OrganizadorId).aprobados,
                Editado: await YaAprobadoAsync(TipoObjetivoModeracion.Evento, e.Id),
                EsPropio: e.OrganizadorId == moderador.Id));
            var pendientesLugares = lugares.Result.Select(async l =>
            {
                bool editado = await YaAprobadoAsync(TipoObjetivoModeracion.Lugar, l.Id);
                return new PendienteModeracion(
                    TipoObjetivoModeracion.Lugar, null, l, nombresLugares.Single(o => o.id == l.DuenoId).nombre ?? "Organizador",
                    Enviado(TipoObjetivoModeracion.Lugar, l.Id), Nuevo: !editado, Editado: editado, EsPropio: l.DuenoId == moderador.Id);
            });
            var pendientes = await Task.WhenAll(pendientesEventos.Concat(pendientesLugares));

            return new DatosBandeja(
                [.. pendientes.OrderBy(p => p.EnviadoEn).ThenBy(p => p.Id)],
                organizadores.Result.Count,
                reportes.Result.Count,
                historial.Result.Count(r => r.DecididoEn.Date == DateTime.Today));
        }

        // ---------------------------------------------------------------- Revisar evento

        /// <summary>Vista previa del evento, datos del organizador y la cola de eventos pendientes ("1 de 4").</summary>
        public async Task<DatosRevision> ObtenerRevisionAsync(int eventoId)
        {
            Usuario moderador = Moderador();
            var detalle = bd.Eventos.ObtenerAsync(eventoId);
            var envios = bd.Moderacion.ListarEnviosPendientesAsync();
            var editado = YaAprobadoAsync(TipoObjetivoModeracion.Evento, eventoId);
            await Task.WhenAll(detalle, envios, editado);

            EventoDetalle evento = detalle.Result ?? throw new ReglaNegocioException("Este evento ya no existe.");
            EventoTarjeta e = evento.Evento;
            if (e.Estado != EstadoEvento.Pendiente)
                throw new ReglaNegocioException("Este evento ya no está pendiente de revisión.");

            int organizadorId = e.OrganizadorId;
            var perfil = bd.Organizadores.ObtenerAsync(organizadorId);
            var aprobacion = bd.Moderacion.ObtenerUltimaDecisionAsync(TipoObjetivoModeracion.Organizador, organizadorId);
            var suyos = bd.Eventos.ListarDeOrganizadorAsync(organizadorId);
            var lugares = bd.Lugares.ListarDeDuenoAsync(organizadorId);
            var abiertos = bd.Reportes.ListarAsync(EstadoReporte.Abierto);
            var resueltos = bd.Reportes.ListarAsync(EstadoReporte.Resuelto);
            await Task.WhenAll(perfil, aprobacion, suyos, lugares, abiertos, resueltos);

            // Historial del organizador sin contar el evento que se revisa.
            var otros = suyos.Result.Where(x => x.Id != eventoId).ToList();
            var eventosIds = suyos.Result.Select(x => x.Id).ToHashSet();
            var lugaresIds = lugares.Result.Select(x => x.Id).ToHashSet();
            int reportes = abiertos.Result.Concat(resueltos.Result).Count(r =>
                (r.Tipo == TipoObjetivoReporte.Evento && eventosIds.Contains(r.ObjetivoId)) ||
                (r.Tipo == TipoObjetivoReporte.Lugar && lugaresIds.Contains(r.ObjetivoId)));
            int aprobados = otros.Count(Publicado);

            var cola = envios.Result.Where(x => x.Tipo == TipoObjetivoModeracion.Evento)
                .OrderBy(x => x.EnviadoEn).ThenBy(x => x.ObjetivoId).ToList();
            DateTime? enviadoEn = cola.FirstOrDefault(x => x.ObjetivoId == eventoId)?.EnviadoEn;

            var organizador = new OrganizadorRevision(
                perfil.Result?.NombreNegocio ?? e.OrganizadorNombre,
                aprobacion.Result is { Decision: DecisionModeracion.Aprobado } a ? a.DecididoEn : null,
                aprobados,
                otros.Count(x => x.Estado == EstadoEvento.Rechazado),
                reportes,
                perfil.Result?.RedSocialUrl,
                perfil.Result?.WhatsApp ?? e.OrganizadorWhatsApp);

            return new DatosRevision(
                evento, enviadoEn, organizador,
                PrimerEvento: aprobados == 0,
                Editado: editado.Result,
                EsPropio: organizadorId == moderador.Id,
                Cola: [.. cola.Select(x => x.ObjetivoId)]);
        }

        /// <summary>"Aprobar y publicar". Con <paramref name="destacar"/> entra al final del carrusel del inicio.</summary>
        /// <returns>false si se aprobó pero no se pudo destacar.</returns>
        public async Task<bool> AprobarAsync(int eventoId, string? comentario, bool destacar)
        {
            Usuario moderador = Moderador();
            await bd.Moderacion.AprobarAsync(moderador.Id, TipoObjetivoModeracion.Evento, eventoId, comentario);
            if (!destacar) return true;

            var destacados = await bd.Eventos.ListarDestacadosAsync(limite: 100);
            int orden = destacados.Select(x => x.OrdenDestacado ?? 0).DefaultIfEmpty(0).Max() + 1;
            return await bd.Moderacion.DestacarEventoAsync(eventoId, orden);
        }

        /// <summary>
        /// "Rechazar". El organizador ve el texto guardado ("Motivo de rechazo"): el motivo elegido y el
        /// comentario, salvo "Otro", que deja solo el comentario.
        /// </summary>
        public Task RechazarAsync(int eventoId, string motivo, string comentario)
        {
            Usuario moderador = Moderador();
            comentario = comentario.Trim();
            string texto = motivo == MotivoOtro ? comentario : $"{motivo}: {comentario}";
            return bd.Moderacion.RechazarAsync(moderador.Id, TipoObjetivoModeracion.Evento, eventoId, texto);
        }

        // ---------------------------------------------------------------- Equipo (solo supermoderadores)

        /// <summary>Moderadores activos para Paginas/EquipoModeracion.razor.</summary>
        public Task<List<Usuario>> ListarEquipoAsync()
        {
            SuperModerador();
            return bd.Usuarios.ListarModeradoresAsync();
        }

        /// <summary>Elimina (desactiva) la cuenta de un moderador. BackendLogica vuelve a revisar el permiso.</summary>
        public async Task EliminarModeradorAsync(int moderadorId)
        {
            Usuario super = SuperModerador();
            if (!await bd.Usuarios.DesactivarModeradorAsync(super.Id, moderadorId))
                throw new ReglaNegocioException("Esa cuenta ya no se puede eliminar.");
        }

        // ----------------------------------------------------------------

        /// <summary>Aprobado y vigente, terminado o cancelado: alguna vez se publicó.</summary>
        private static bool Publicado(EventoTarjeta e) => e.Estado is EstadoEvento.Aprobado or EstadoEvento.Cancelado;

        /// <summary>La última decisión fue "aprobado": el autor lo editó y volvió a revisión.</summary>
        private async Task<bool> YaAprobadoAsync(TipoObjetivoModeracion tipo, int id) =>
            await bd.Moderacion.ObtenerUltimaDecisionAsync(tipo, id) is { Decision: DecisionModeracion.Aprobado };

        private Usuario Moderador()
        {
            Usuario usuario = sesion.Requerir("moderar publicaciones");
            return usuario.EsModerador ? usuario : throw new ReglaNegocioException("Solo los moderadores pueden entrar a Moderación.");
        }

        private Usuario SuperModerador()
        {
            Usuario usuario = sesion.Requerir("administrar el equipo de moderación");
            return usuario.EsSuperModerador ? usuario : throw new ReglaNegocioException("Solo un supermoderador puede administrar el equipo.");
        }
    }

    /// <param name="Pendientes">Eventos y lugares por revisar, los más antiguos primero.</param>
    /// <param name="OrganizadoresPendientes">Pestaña "Organizadores".</param>
    /// <param name="ReportesAbiertos">Pestaña "Reportes" y la métrica "Reportes abiertos".</param>
    /// <param name="DecisionesHoy">Métrica "Decisiones hoy" (de todos los moderadores).</param>
    public sealed record DatosBandeja(
        IReadOnlyList<PendienteModeracion> Pendientes,
        int OrganizadoresPendientes,
        int ReportesAbiertos,
        int DecisionesHoy);

    /// <summary>Un renglón de la bandeja: un evento (<see cref="Evento"/>) o un lugar (<see cref="Lugar"/>).</summary>
    /// <param name="Autor">Organizador del evento o negocio dueño del lugar.</param>
    /// <param name="Nuevo">"Primer evento" (su organizador no tiene eventos publicados) o "Lugar nuevo".</param>
    /// <param name="Editado">"Editado: vuelve a revisión": ya estaba aprobado y su autor lo cambió.</param>
    /// <param name="EsPropio">"Es tu evento: lo revisa otro moderador".</param>
    public sealed record PendienteModeracion(
        TipoObjetivoModeracion Tipo,
        EventoTarjeta? Evento,
        LugarTarjeta? Lugar,
        string Autor,
        DateTime EnviadoEn,
        bool Nuevo,
        bool Editado,
        bool EsPropio)
    {
        public int Id => Evento?.Id ?? Lugar!.Id;
        public string Titulo => Evento?.Titulo ?? Lugar!.Nombre;
        public string? ImagenUrl => Evento != null ? Evento.ImagenUrl : Lugar!.ImagenUrl;
    }

    /// <param name="EnviadoEn">Cuándo se mandó a revisión (null si ya no está en la cola).</param>
    /// <param name="Cola">Ids de los eventos pendientes en orden, para "1 de 4" y las flechas.</param>
    public sealed record DatosRevision(
        EventoDetalle Detalle,
        DateTime? EnviadoEn,
        OrganizadorRevision Organizador,
        bool PrimerEvento,
        bool Editado,
        bool EsPropio,
        IReadOnlyList<int> Cola)
    {
        public EventoTarjeta Evento => Detalle.Evento;
    }

    /// <summary>Tarjeta "Organizador" de Revisar evento.</summary>
    /// <param name="AprobadoEn">"Aprobado como organizador el 12 sep".</param>
    /// <param name="Aprobados">Sus otros eventos publicados.</param>
    /// <param name="Reportes">Reportes (abiertos o resueltos) sobre sus eventos y lugares.</param>
    public sealed record OrganizadorRevision(
        string Nombre,
        DateTime? AprobadoEn,
        int Aprobados,
        int Rechazados,
        int Reportes,
        string? RedSocialUrl,
        string? WhatsApp);
}
