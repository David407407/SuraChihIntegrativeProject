namespace BackendLogica.Modelos
{
    /// <summary>Reseña de un evento o de un lugar (misma forma en ambas tablas).</summary>
    public sealed record Resena(
        int Id,
        TipoResena Tipo,
        int ObjetivoId,
        int UsuarioId,
        string NombreUsuario,
        string? AvatarUrl,
        int Calificacion,
        string? Comentario,
        string? Respuesta,
        DateTime? RespondidaEn,
        bool Oculta,
        DateTime CreadaEn);

    /// <summary>Reporte que envía un usuario sobre un evento, lugar o reseña.</summary>
    public sealed record Reporte(
        int Id,
        int ReportanteId,
        TipoObjetivoReporte Tipo,
        int ObjetivoId,
        MotivoReporte Motivo,
        string? Detalles,
        EstadoReporte Estado,
        int? ResueltoPor,
        DateTime? ResueltoEn,
        DateTime CreadoEn);

    /// <summary>
    /// Reporte con lo que muestra "Moderación / Reportes". Siempre apunta a su evento o lugar: el reportado o,
    /// si reportaron una reseña, aquel donde se escribió.
    /// </summary>
    /// <param name="ObjetivoNombre">Título del evento o nombre del lugar.</param>
    /// <param name="ResenaAutor">Solo en reportes de reseña, igual que los tres siguientes.</param>
    public sealed record ReporteDetalle(
        Reporte Reporte,
        string ReportanteNombre,
        int? EventoId,
        int? LugarId,
        string ObjetivoNombre,
        string? ResenaAutor = null,
        int? ResenaCalificacion = null,
        string? ResenaComentario = null,
        bool ResenaOculta = false);

    /// <summary>Una reseña que un moderador ocultó o volvió a mostrar (tabla review_moderation).</summary>
    public sealed record RegistroResena(
        int Id,
        int ModeradorId,
        string ModeradorNombre,
        TipoResena Tipo,
        int ResenaId,
        int ObjetivoId,
        string ObjetivoNombre,
        string AutorNombre,
        int Calificacion,
        string? Comentario,
        AccionResena Accion,
        DateTime DecididoEn);

    /// <summary>Una decisión del historial de moderación.</summary>
    public sealed record RegistroModeracion(
        int Id,
        int ModeradorId,
        string ModeradorNombre,
        TipoObjetivoModeracion Tipo,
        int ObjetivoId,
        DecisionModeracion Decision,
        string? Comentario,
        DateTime DecididoEn,
        string ObjetivoNombre = "");

    /// <summary>Evento o lugar pendiente de moderación y cuándo se mandó a revisión.</summary>
    public sealed record EnvioPendiente(TipoObjetivoModeracion Tipo, int ObjetivoId, DateTime EnviadoEn);

    /// <summary>Cifras generales para el hero de la landing ("21 eventos esta semana"...).</summary>
    public sealed record ResumenPlataforma(int EventosEstaSemana, int LugaresParaDescubrir, int Categorias);
}
