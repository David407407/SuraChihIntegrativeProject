namespace BackendLogica.Modelos
{
    /// <summary>Lugar (local) listo para tarjetas, mapa y detalle.</summary>
    public sealed record LugarTarjeta
    {
        public required int Id { get; init; }
        public required string Nombre { get; init; }
        public string? Descripcion { get; init; }
        public required string Direccion { get; init; }
        public decimal Latitud { get; init; }
        public decimal Longitud { get; init; }
        public string? Telefono { get; init; }
        public string? SitioWebUrl { get; init; }
        /// <summary>Rango de precio "$100–200". Ambos null = sin dato; 0 = gratis.</summary>
        public decimal? PrecioMin { get; init; }
        public decimal? PrecioMax { get; init; }
        /// <summary>Foto principal (URL de Cloudinary). Puede ser null mientras no se suba.</summary>
        public string? ImagenUrl { get; init; }
        public EstadoLugar Estado { get; init; }
        public int DuenoId { get; init; }

        /// <summary>Promedio de reseñas visibles (null si no tiene).</summary>
        public decimal? Calificacion { get; init; }
        public int NumResenas { get; init; }

        public IReadOnlyList<Etiqueta> Etiquetas { get; init; } = [];
        public IReadOnlyList<HorarioDia> Horario { get; init; } = [];
        /// <summary>Si está abierto en el momento de la consulta y hasta qué hora.</summary>
        public EstadoHorario HorarioAhora { get; init; } = EstadoHorario.Cerrado;
    }

    /// <summary>Lugar con su galería de fotos (pantalla de detalle).</summary>
    public sealed record LugarDetalle(LugarTarjeta Lugar, IReadOnlyList<string> Galeria);

    /// <summary>Datos del formulario para registrar o editar un lugar.</summary>
    public sealed record DatosLugar
    {
        public required string Nombre { get; init; }
        public string? Descripcion { get; init; }
        public required string Direccion { get; init; }
        public required decimal Latitud { get; init; }
        public required decimal Longitud { get; init; }
        public string? Telefono { get; init; }
        public string? SitioWebUrl { get; init; }
        public decimal? PrecioMin { get; init; }
        public decimal? PrecioMax { get; init; }
        public string? ImagenUrl { get; init; }
        public IReadOnlyCollection<int> EtiquetaIds { get; init; } = [];
        public IReadOnlyCollection<HorarioDia> Horario { get; init; } = [];
    }

    /// <summary>
    /// Horario de un día. Si <see cref="Cierra"/> es menor que <see cref="Abre"/>, cierra después de medianoche.
    /// </summary>
    public sealed record HorarioDia(DayOfWeek Dia, TimeSpan Abre, TimeSpan Cierra)
    {
        public bool CruzaMedianoche => Cierra < Abre;
    }

    /// <summary>Estado de apertura calculado para un momento dado.</summary>
    /// <param name="Abierto">Está abierto ahora.</param>
    /// <param name="Hasta">Si está abierto, hora de cierre.</param>
    /// <param name="AbreA">Si está cerrado pero abre más tarde hoy, hora de apertura.</param>
    public sealed record EstadoHorario(bool Abierto, TimeSpan? Hasta, TimeSpan? AbreA)
    {
        public static readonly EstadoHorario Cerrado = new(false, null, null);

        /// <summary>Calcula si un lugar está abierto en <paramref name="momento"/> según su horario semanal.</summary>
        public static EstadoHorario Calcular(IEnumerable<HorarioDia> horario, DateTime momento)
        {
            var porDia = horario.ToDictionary(h => h.Dia);
            TimeSpan hora = momento.TimeOfDay;

            // Un horario de ayer que cruza medianoche puede seguir abierto (ej. cierra a la 1:00).
            DayOfWeek ayer = (DayOfWeek)(((int)momento.DayOfWeek + 6) % 7);
            if (porDia.TryGetValue(ayer, out var deAyer) && deAyer.CruzaMedianoche && hora < deAyer.Cierra)
                return new EstadoHorario(true, deAyer.Cierra, null);

            if (!porDia.TryGetValue(momento.DayOfWeek, out var deHoy))
                return Cerrado;

            bool abierto = deHoy.CruzaMedianoche
                ? hora >= deHoy.Abre
                : hora >= deHoy.Abre && hora < deHoy.Cierra;

            if (abierto) return new EstadoHorario(true, deHoy.Cierra, null);
            return hora < deHoy.Abre ? new EstadoHorario(false, null, deHoy.Abre) : Cerrado;
        }
    }
}
