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

    /// <summary>Una decisión del historial de moderación.</summary>
    public sealed record RegistroModeracion(
        int Id,
        int ModeradorId,
        string ModeradorNombre,
        TipoObjetivoModeracion Tipo,
        int ObjetivoId,
        DecisionModeracion Decision,
        string? Comentario,
        DateTime DecididoEn);

    /// <summary>Cifras generales para el hero de la landing ("21 eventos esta semana"...).</summary>
    public sealed record ResumenPlataforma(int EventosEstaSemana, int LugaresParaDescubrir, int Categorias);
}
