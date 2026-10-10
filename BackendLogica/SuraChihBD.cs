using BackendLogica.Configuracion;
using BackendLogica.Modelos;
using BackendLogica.Repositorios;

namespace BackendLogica
{
    /// <summary>
    /// Punto de entrada único a la base de datos. El frontend crea uno y accede a cada área por su propiedad:
    /// <code>
    /// var bd = new SuraChihBD();
    /// var destacados = await bd.Eventos.ListarDestacadosAsync();
    /// var usuario = await bd.Usuarios.IniciarSesionAsync("mariana_r", "Test1234!");
    /// </code>
    /// Todos los repositorios comparten la misma <see cref="ConfiguracionBD"/>.
    /// </summary>
    public sealed class SuraChihBD
    {
        public ConfiguracionBD Configuracion { get; }

        public UsuarioDB Usuarios { get; }
        public RecuperacionContrasenaDB RecuperacionContrasena { get; }
        public OrganizadorDB Organizadores { get; }
        public EtiquetaDB Etiquetas { get; }
        public EventoDB Eventos { get; }
        public LugarDB Lugares { get; }
        public FavoritoDB Favoritos { get; }
        public InscripcionDB Inscripciones { get; }
        public ResenaDB Resenas { get; }
        public ReporteDB Reportes { get; }
        public ModeracionDB Moderacion { get; }

        public SuraChihBD(ConfiguracionBD? configuracion = null)
        {
            Configuracion = configuracion ?? ConfiguracionBD.Predeterminada;
            Usuarios = new UsuarioDB(Configuracion);
            RecuperacionContrasena = new RecuperacionContrasenaDB(Configuracion);
            Organizadores = new OrganizadorDB(Configuracion);
            Etiquetas = new EtiquetaDB(Configuracion);
            Eventos = new EventoDB(Configuracion);
            Lugares = new LugarDB(Configuracion);
            Favoritos = new FavoritoDB(Configuracion);
            Inscripciones = new InscripcionDB(Configuracion);
            Resenas = new ResenaDB(Configuracion);
            Reportes = new ReporteDB(Configuracion);
            Moderacion = new ModeracionDB(Configuracion);
        }

        /// <summary>true si MySQL responde con la configuración actual.</summary>
        public Task<bool> ProbarConexionAsync(CancellationToken ct = default) => Etiquetas.ProbarConexionAsync(ct);

        /// <summary>Cifras del hero de la landing: eventos de esta semana, lugares y categorías.</summary>
        public async Task<ResumenPlataforma> ObtenerResumenAsync(CancellationToken ct = default)
        {
            var eventos = Eventos.ContarEstaSemanaAsync(ct);
            var lugares = Lugares.ContarAprobadosAsync(ct);
            var categorias = Etiquetas.ContarAsync(AlcanceEtiqueta.Evento, ct);
            await Task.WhenAll(eventos, lugares, categorias);
            return new ResumenPlataforma(eventos.Result, lugares.Result, categorias.Result);
        }
    }
}
