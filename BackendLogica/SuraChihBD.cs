using BackendLogica.Configuracion;
using BackendLogica.Contratos;
using BackendLogica.Modelos;
using BackendLogica.Repositorios;

namespace BackendLogica
{
    /// <summary>
    /// Punto de entrada único a la base de datos MySQL. El frontend accede a cada área por su propiedad:
    /// <code>
    /// ISuraChihBD bd = FabricaBD.Crear();   // real o falso según dbsettings.local.json
    /// var destacados = await bd.Eventos.ListarDestacadosAsync();
    /// var usuario = await bd.Usuarios.IniciarSesionAsync("mariana_r", "Test1234!");
    /// </code>
    /// Todos los repositorios comparten la misma <see cref="ConfiguracionBD"/>.
    /// </summary>
    public sealed class SuraChihBD : ISuraChihBD
    {
        private readonly EtiquetaDB _etiquetas;

        public ConfiguracionBD Configuracion { get; }

        public IUserRepository Usuarios { get; }
        public IPasswordResetRepository RecuperacionContrasena { get; }
        public IOrganizerRepository Organizadores { get; }
        public ITagRepository Etiquetas => _etiquetas;
        public IEventRepository Eventos { get; }
        public IPlaceRepository Lugares { get; }
        public IFavoriteRepository Favoritos { get; }
        public IInscriptionRepository Inscripciones { get; }
        public IReviewRepository Resenas { get; }
        public IReportRepository Reportes { get; }
        public IModerationRepository Moderacion { get; }

        public SuraChihBD(ConfiguracionBD? configuracion = null)
        {
            Configuracion = configuracion ?? ConfiguracionBD.Predeterminada;
            _etiquetas = new EtiquetaDB(Configuracion);
            Usuarios = new UsuarioDB(Configuracion);
            RecuperacionContrasena = new RecuperacionContrasenaDB(Configuracion);
            Organizadores = new OrganizadorDB(Configuracion);
            Eventos = new EventoDB(Configuracion);
            Lugares = new LugarDB(Configuracion);
            Favoritos = new FavoritoDB(Configuracion);
            Inscripciones = new InscripcionDB(Configuracion);
            Resenas = new ResenaDB(Configuracion);
            Reportes = new ReporteDB(Configuracion);
            Moderacion = new ModeracionDB(Configuracion);
        }

        /// <summary>true si MySQL responde con la configuración actual.</summary>
        public Task<bool> ProbarConexionAsync(CancellationToken ct = default) => _etiquetas.ProbarConexionAsync(ct);

        /// <summary>Cifras del hero de la landing: eventos de esta semana, lugares y categorías.</summary>
        public Task<ResumenPlataforma> ObtenerResumenAsync(CancellationToken ct = default) => Resumir(this, ct);

        /// <summary>Mismo cálculo para el origen real y el falso.</summary>
        internal static async Task<ResumenPlataforma> Resumir(ISuraChihBD bd, CancellationToken ct)
        {
            var eventos = bd.Eventos.ContarEstaSemanaAsync(ct);
            var lugares = bd.Lugares.ContarAprobadosAsync(ct);
            var categorias = bd.Etiquetas.ContarAsync(AlcanceEtiqueta.Evento, ct);
            await Task.WhenAll(eventos, lugares, categorias);
            return new ResumenPlataforma(eventos.Result, lugares.Result, categorias.Result);
        }
    }
}
