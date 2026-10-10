namespace BackendLogica.Modelos
{
    /// <summary>
    /// Evento listo para mostrarse en tarjetas y en el detalle. Sale de la vista <c>v_event_card</c>,
    /// que ya resuelve la ubicación (la del evento o la de su lugar) y los datos del organizador.
    /// </summary>
    public sealed record EventoTarjeta
    {
        public required int Id { get; init; }
        public required string Titulo { get; init; }
        public required string Descripcion { get; init; }
        public required DateTime Inicio { get; init; }
        public required DateTime Fin { get; init; }

        public decimal PrecioMin { get; init; }
        public decimal PrecioMax { get; init; }
        public bool EsGratis { get; init; }
        /// <summary>Página oficial para comprar boletos (la app no vende boletos).</summary>
        public string? BoletosUrl { get; init; }
        /// <summary>Foto principal (URL de Cloudinary). Puede ser null mientras no se suba.</summary>
        public string? ImagenUrl { get; init; }

        public EstadoEvento Estado { get; init; }
        /// <summary>Aprobado y ya terminó.</summary>
        public bool Terminado { get; init; }
        /// <summary>Aparece en el carrusel del inicio (lo deciden los moderadores).</summary>
        public bool Destacado { get; init; }
        public int? OrdenDestacado { get; init; }

        public int? LugarId { get; init; }
        public string? LugarNombre { get; init; }
        public string? Direccion { get; init; }
        public decimal? Latitud { get; init; }
        public decimal? Longitud { get; init; }

        public int OrganizadorId { get; init; }
        public string OrganizadorNombre { get; init; } = "";
        public string OrganizadorWhatsApp { get; init; } = "";
        public bool OrganizadorVerificado { get; init; }

        /// <summary>Cuántas personas lo marcaron con corazón.</summary>
        public int Interesados { get; init; }
        public IReadOnlyList<Etiqueta> Etiquetas { get; init; } = [];
    }

    /// <summary>Evento con su galería de fotos (pantalla de detalle).</summary>
    public sealed record EventoDetalle(EventoTarjeta Evento, IReadOnlyList<string> Galeria);

    /// <summary>Datos del formulario "Publicar evento".</summary>
    /// <remarks>
    /// Si <see cref="LugarId"/> es null (parque, explanada), la dirección y las coordenadas son obligatorias.
    /// Precio 0–0 = gratis.
    /// </remarks>
    public sealed record DatosEvento
    {
        public int? LugarId { get; init; }
        public required string Titulo { get; init; }
        public required string Descripcion { get; init; }
        public required DateTime Inicio { get; init; }
        public required DateTime Fin { get; init; }
        public string? Direccion { get; init; }
        public decimal? Latitud { get; init; }
        public decimal? Longitud { get; init; }
        public decimal PrecioMin { get; init; }
        public decimal PrecioMax { get; init; }
        public string? BoletosUrl { get; init; }
        public string? ImagenUrl { get; init; }
        public IReadOnlyCollection<int> EtiquetaIds { get; init; } = [];
    }

    /// <summary>Embudo del panel del organizador (vista <c>v_event_stats</c>).</summary>
    public sealed record EstadisticasEvento(
        int EventoId,
        int Vistas,
        int Interesados,
        int Inscritos,
        decimal? Calificacion,
        int NumResenas);

    /// <summary>Evento en "Mis planes" con el estado de la inscripción del usuario.</summary>
    public sealed record PlanUsuario(
        EventoTarjeta Evento,
        EstadoInscripcion Estado,
        DateTime InscritoEn,
        DateTime? CanceladoEn);
}
