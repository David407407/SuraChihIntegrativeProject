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
}
