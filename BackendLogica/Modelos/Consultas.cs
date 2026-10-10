namespace BackendLogica.Modelos
{
    /// <summary>
    /// Modal "Filtros" y chips del inicio (Hoy, Este fin de semana, Gratis...). Todos los criterios se combinan;
    /// los que se dejan en su valor por defecto no filtran.
    /// </summary>
    public sealed record FiltroEventos
    {
        /// <summary>Texto del buscador (título, lugar o etiqueta). Null o vacío = sin texto.</summary>
        public string? Texto { get; init; }
        /// <summary>Eventos que tengan AL MENOS UNA de estas etiquetas. Vacío = todas.</summary>
        public IReadOnlyCollection<int> EtiquetaIds { get; init; } = [];
        public FiltroPrecio Precio { get; init; } = FiltroPrecio.Todos;
        public FiltroCuando Cuando { get; init; } = FiltroCuando.Cualquiera;
        /// <summary>Solo con <see cref="FiltroCuando.Rango"/> (modal Calendario): primer día, inclusive.</summary>
        public DateTime? Desde { get; init; }
        /// <summary>Solo con <see cref="FiltroCuando.Rango"/>: último día, inclusive.</summary>
        public DateTime? Hasta { get; init; }
        public int Limite { get; init; } = 30;

        /// <summary>
        /// Convierte <see cref="Cuando"/> en un rango [desde, hasta) a partir de <paramref name="ahora"/>.
        /// Un evento entra si se cruza con el rango (los de varios días cuentan aunque hayan empezado antes).
        /// </summary>
        /// <returns>null si no se filtra por fecha.</returns>
        public (DateTime Desde, DateTime Hasta)? RangoFechas(DateTime ahora)
        {
            DateTime hoy = ahora.Date;
            return Cuando switch
            {
                FiltroCuando.Hoy => (hoy, hoy.AddDays(1)),
                FiltroCuando.Manana => (hoy.AddDays(1), hoy.AddDays(2)),
                // Sábado y domingo; si ya es fin de semana, desde hoy.
                FiltroCuando.EsteFinDeSemana => ahora.DayOfWeek switch
                {
                    DayOfWeek.Saturday => (hoy, hoy.AddDays(2)),
                    DayOfWeek.Sunday => (hoy, hoy.AddDays(1)),
                    _ => (hoy.AddDays(DayOfWeek.Saturday - ahora.DayOfWeek), hoy.AddDays(DayOfWeek.Saturday - ahora.DayOfWeek + 2))
                },
                // De lunes a domingo de la semana que sigue.
                FiltroCuando.ProximaSemana => (LunesSiguiente(hoy), LunesSiguiente(hoy).AddDays(7)),
                FiltroCuando.Rango when Desde.HasValue || Hasta.HasValue =>
                    ((Desde ?? hoy).Date, (Hasta ?? Desde ?? hoy).Date.AddDays(1)),
                _ => null
            };
        }

        private static DateTime LunesSiguiente(DateTime hoy)
        {
            int dias = ((int)DayOfWeek.Monday - (int)hoy.DayOfWeek + 7) % 7;
            return hoy.AddDays(dias == 0 ? 7 : dias);
        }
    }

    /// <summary>Barra de la gráfica "Últimos 7 días" de las estadísticas del evento.</summary>
    public sealed record VistasDia(DateOnly Dia, int Vistas);

    /// <summary>Fila de la tabla "Inscritos" de las estadísticas del evento.</summary>
    public sealed record Inscrito(
        int UsuarioId,
        string NombreUsuario,
        string? AvatarUrl,
        EstadoInscripcion Estado,
        DateTime InscritoEn,
        DateTime? CanceladoEn);

    /// <summary>
    /// Bloque de la pantalla "Reseñas": promedio, total y barras por estrellas (solo reseñas visibles).
    /// </summary>
    /// <param name="PorEstrellas">Llaves 1 a 5, siempre presentes (0 si no hay reseñas con esa calificación).</param>
    public sealed record ResumenResenas(decimal? Promedio, int Total, IReadOnlyDictionary<int, int> PorEstrellas);

    /// <summary>Calificación de un lugar (vista <c>v_place_rating</c>). Promedio null = sin reseñas.</summary>
    public sealed record CalificacionLugar(int LugarId, decimal? Calificacion, int NumResenas);
}
