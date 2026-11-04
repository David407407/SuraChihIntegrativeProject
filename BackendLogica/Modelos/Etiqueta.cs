namespace BackendLogica.Modelos
{
    /// <summary>Etiqueta o categoría (los chips del frontend: Familiar, Música, Cafetería...).</summary>
    /// <param name="Icono">Nombre del recurso de icono en la app (family, music, coffee...).</param>
    /// <param name="ColorHex">Color del icono del chip, formato #RRGGBB.</param>
    public sealed record Etiqueta(
        int Id,
        string Nombre,
        string Icono,
        string ColorHex,
        AlcanceEtiqueta Alcance,
        int Orden);

    /// <summary>Gustos del usuario (tabla <c>user_interest</c>): las categorías que eligió.</summary>
    /// <param name="EtiquetaIds">Solo etiquetas activas.</param>
    /// <param name="Avisos">Aceptó que le avisemos de eventos nuevos de sus gustos (<c>user.notify_interests</c>).</param>
    public sealed record Gustos(IReadOnlySet<int> EtiquetaIds, bool Avisos)
    {
        public static readonly Gustos Ninguno = new(new HashSet<int>(), false);
    }
}
