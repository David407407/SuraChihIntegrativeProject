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

        /// <summary>Motivos del modal "Rechazar" cuando es una solicitud de organizador.</summary>
        public static readonly string[] MotivosRechazoOrganizador =
        [
            "La red social no existe o no tiene actividad",
            "El nombre no coincide con la red social",
            "No se pudo verificar que sea real",
            "Otro",
        ];

        public const string MotivoOtro = "Otro";

        /// <summary>Filas que muestra "Moderación / Historial" al entrar y las que suma "Ver más".</summary>
        public const int FilasHistorial = 50;

        /// <summary>Filas de "Decididas recientemente" en Moderación / Organizadores.</summary>
        private const int DecididasRecientes = 5;

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
            return bd.Moderacion.RechazarAsync(moderador.Id, TipoObjetivoModeracion.Evento, eventoId, TextoRechazo(motivo, comentario));
        }

        // ---------------------------------------------------------------- Solicitudes de organizador

        /// <summary>"Moderación / Organizadores": las solicitudes pendientes (las más antiguas primero) y las decididas.</summary>
        public async Task<DatosOrganizadores> ObtenerOrganizadoresAsync()
        {
            Usuario moderador = Moderador();
            var pendientes = bd.Organizadores.ListarPorEstadoAsync(EstadoOrganizador.Pendiente);
            var historial = bd.Moderacion.ListarHistorialAsync(HistorialHoy);
            var envios = bd.Moderacion.ListarEnviosPendientesAsync();
            var reportes = bd.Reportes.ListarAsync(EstadoReporte.Abierto);
            await Task.WhenAll(pendientes, historial, envios, reportes);

            var solicitudes = await Task.WhenAll(pendientes.Result.Select(async p =>
            {
                Usuario? usuario = await bd.Usuarios.ObtenerAsync(p.UsuarioId);
                return new SolicitudPendiente(p, usuario?.NombreUsuario ?? "", usuario?.CreadoEn, EsPropia: p.UsuarioId == moderador.Id);
            }));
            var decididas = await Task.WhenAll(historial.Result
                .Where(r => r.Tipo == TipoObjetivoModeracion.Organizador)
                .Take(DecididasRecientes)
                .Select(async r => new DecisionOrganizador(
                    (await bd.Organizadores.ObtenerAsync(r.ObjetivoId))?.NombreNegocio ?? "Organizador",
                    r.DecididoEn, r.ModeradorNombre, r.Decision)));

            return new DatosOrganizadores(solicitudes, decididas, envios.Result.Count, reportes.Result.Count);
        }

        /// <summary>Aprueba la solicitud: el usuario ya puede publicar eventos y registrar lugares.</summary>
        public Task AprobarOrganizadorAsync(int usuarioId) =>
            bd.Moderacion.AprobarAsync(Moderador().Id, TipoObjetivoModeracion.Organizador, usuarioId);

        /// <summary>Rechaza la solicitud. El solicitante ve el texto en "Hazte organizador" y puede reenviarla.</summary>
        public Task RechazarOrganizadorAsync(int usuarioId, string motivo, string comentario) =>
            bd.Moderacion.RechazarAsync(Moderador().Id, TipoObjetivoModeracion.Organizador, usuarioId, TextoRechazo(motivo, comentario));

        // ---------------------------------------------------------------- Reportes

        /// <summary>"Moderación / Reportes": los reportes en ese estado, juntando los repetidos del mismo objetivo.</summary>
        public async Task<DatosReportes> ObtenerReportesAsync(EstadoReporte estado)
        {
            Moderador();
            var lista = bd.Reportes.ListarDetalleAsync(estado);
            var conteos = ContarAsync();
            await Task.WhenAll(lista, conteos);
            var grupos = lista.Result.GroupBy(r => (r.Reporte.Tipo, r.Reporte.ObjetivoId)).Select(g => new GrupoReportes([.. g])).ToList();
            return new DatosReportes(grupos, conteos.Result);
        }

        /// <summary>"Descartar" o "Marcar resuelto": cierra todos los reportes del grupo.</summary>
        public async Task CerrarReportesAsync(GrupoReportes grupo, EstadoReporte resultado)
        {
            int moderadorId = Moderador().Id;
            foreach (var r in grupo.Reportes) await bd.Reportes.ResolverAsync(r.Reporte.Id, moderadorId, resultado);
        }

        /// <summary>"Reabrir": vuelve a abrir los reportes del grupo.</summary>
        public async Task ReabrirReportesAsync(GrupoReportes grupo)
        {
            int moderadorId = Moderador().Id;
            foreach (var r in grupo.Reportes) await bd.Reportes.ReabrirAsync(r.Reporte.Id, moderadorId);
        }

        /// <summary>"Ocultar reseña": la oculta y cierra sus reportes abiertos. Si ya estaba oculta, solo cierra los del grupo.</summary>
        public async Task OcultarResenaAsync(GrupoReportes grupo)
        {
            int moderadorId = Moderador().Id;
            Reporte reporte = grupo.Principal.Reporte;
            TipoResena tipo = reporte.Tipo == TipoObjetivoReporte.ResenaEvento ? TipoResena.Evento : TipoResena.Lugar;
            if (!await bd.Resenas.CambiarVisibilidadAsync(moderadorId, tipo, reporte.ObjetivoId, true))
                await CerrarReportesAsync(grupo, EstadoReporte.Resuelto);
        }

        /// <summary>"Volver a mostrar" una reseña oculta. Queda en el historial de reseñas.</summary>
        /// <returns>false si ya estaba visible (otro moderador se adelantó).</returns>
        public Task<bool> MostrarResenaAsync(TipoResena tipo, int resenaId) =>
            bd.Resenas.CambiarVisibilidadAsync(Moderador().Id, tipo, resenaId, false);

        // ---------------------------------------------------------------- Historial

        /// <summary>"Moderación / Historial": las decisiones más recientes y el total con ese filtro.</summary>
        public async Task<DatosHistorial<RegistroModeracion>> ObtenerHistorialAsync(DecisionModeracion? decision, int limite)
        {
            Moderador();
            var filas = bd.Moderacion.ListarHistorialAsync(limite, decision);
            var total = bd.Moderacion.ContarHistorialAsync(decision);
            var conteos = ContarAsync();
            await Task.WhenAll(filas, total, conteos);
            return new DatosHistorial<RegistroModeracion>(filas.Result, total.Result, conteos.Result);
        }

        /// <summary>"Reseñas moderadas" del historial: un supermoderador ve todo y un moderador solo lo suyo (lo decide BackendLogica).</summary>
        public async Task<DatosHistorial<RegistroResena>> ObtenerHistorialResenasAsync(int limite)
        {
            int moderadorId = Moderador().Id;
            var filas = bd.Moderacion.ListarHistorialResenasAsync(moderadorId, limite);
            var total = bd.Moderacion.ContarHistorialResenasAsync(moderadorId);
            var conteos = ContarAsync();
            await Task.WhenAll(filas, total, conteos);
            return new DatosHistorial<RegistroResena>(filas.Result, total.Result, conteos.Result);
        }

        /// <summary>Los conteos de las pestañas.</summary>
        private async Task<ConteosModeracion> ContarAsync()
        {
            var envios = bd.Moderacion.ListarEnviosPendientesAsync();
            var organizadores = bd.Organizadores.ListarPorEstadoAsync(EstadoOrganizador.Pendiente);
            var reportes = bd.Reportes.ListarAsync(EstadoReporte.Abierto);
            await Task.WhenAll(envios, organizadores, reportes);
            return new ConteosModeracion(envios.Result.Count, organizadores.Result.Count, reportes.Result.Count);
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

        /// <summary>Lo que ve el autor: el motivo elegido y el comentario, salvo "Otro", que deja solo el comentario.</summary>
        private static string TextoRechazo(string motivo, string comentario)
        {
            comentario = comentario.Trim();
            return motivo == MotivoOtro ? comentario : $"{motivo}: {comentario}";
        }

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

    /// <summary>Lo que muestran las pestañas de moderación.</summary>
    public sealed record ConteosModeracion(int Publicaciones, int Organizadores, int Reportes);

    /// <summary>Los reportes de un mismo evento, lugar o reseña: una sola fila en "Moderación / Reportes".</summary>
    public sealed record GrupoReportes(IReadOnlyList<ReporteDetalle> Reportes)
    {
        public ReporteDetalle Principal => Reportes[0];
    }

    /// <param name="Lista">Los reportes del estado elegido, en el orden de BackendLogica.</param>
    public sealed record DatosReportes(IReadOnlyList<GrupoReportes> Lista, ConteosModeracion Conteos);

    /// <param name="Filas">Las más recientes primero.</param>
    /// <param name="Total">Con el mismo filtro ("Mostrando 7 de 214").</param>
    public sealed record DatosHistorial<T>(IReadOnlyList<T> Filas, int Total, ConteosModeracion Conteos);

    /// <param name="Decididas">"Decididas recientemente": las más recientes primero.</param>
    /// <param name="PublicacionesPendientes">Pestaña "Publicaciones".</param>
    /// <param name="ReportesAbiertos">Pestaña "Reportes".</param>
    public sealed record DatosOrganizadores(
        IReadOnlyList<SolicitudPendiente> Pendientes,
        IReadOnlyList<DecisionOrganizador> Decididas,
        int PublicacionesPendientes,
        int ReportesAbiertos);

    /// <summary>Tarjeta de "Moderación / Organizadores".</summary>
    /// <param name="NombreUsuario">"@sofia_c".</param>
    /// <param name="CuentaCreada">"Cuenta creada: hace 3 meses".</param>
    /// <param name="EsPropia">"Es tu solicitud: la revisa otro moderador" (sin botones).</param>
    public sealed record SolicitudPendiente(PerfilOrganizador Perfil, string NombreUsuario, DateTime? CuentaCreada, bool EsPropia);

    /// <summary>Renglón de "Decididas recientemente".</summary>
    public sealed record DecisionOrganizador(string Negocio, DateTime DecididoEn, string ModeradorNombre, DecisionModeracion Decision);

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
