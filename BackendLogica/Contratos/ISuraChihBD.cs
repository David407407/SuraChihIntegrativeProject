using BackendLogica.Modelos;

namespace BackendLogica.Contratos
{
    /// <summary>
    /// Todo lo que las pantallas necesitan de los datos. Las pantallas dependen SOLO de esta interfaz:
    /// <list type="bullet">
    /// <item><see cref="SuraChihBD"/>: MySQL real.</item>
    /// <item><see cref="Falso.SuraChihFalso"/>: datos de prueba en memoria, sin MySQL.</item>
    /// </list>
    /// Para elegir una, usa <see cref="FabricaBD.Crear"/>.
    /// </summary>
    public interface ISuraChihBD
    {
        IUserRepository Usuarios { get; }
        IPasswordResetRepository RecuperacionContrasena { get; }
        IOrganizerRepository Organizadores { get; }
        ITagRepository Etiquetas { get; }
        IEventRepository Eventos { get; }
        IPlaceRepository Lugares { get; }
        IFavoriteRepository Favoritos { get; }
        IInscriptionRepository Inscripciones { get; }
        IReviewRepository Resenas { get; }
        IReportRepository Reportes { get; }
        IModerationRepository Moderacion { get; }

        /// <summary>true si el origen de datos responde (el falso siempre responde).</summary>
        Task<bool> ProbarConexionAsync(CancellationToken ct = default);

        /// <summary>Cifras del hero de la landing: eventos de esta semana, lugares y categorías.</summary>
        Task<ResumenPlataforma> ObtenerResumenAsync(CancellationToken ct = default);
    }
}
