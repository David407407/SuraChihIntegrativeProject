using System.Text.Json;
using MySql.Data.MySqlClient;

namespace BackendLogica.Configuracion
{
    /// <summary>
    /// Datos para conectarse a MySQL. Se leen de <c>dbsettings.local.json</c>
    /// (ignorado por git, cada quien tiene el suyo) y, si no existe, se usan los valores por defecto.
    /// </summary>
    public sealed record ConfiguracionBD
    {
        public const string NombreArchivo = "dbsettings.local.json";

        public string Servidor { get; init; } = "localhost";
        public uint Puerto { get; init; } = 3306;
        public string Usuario { get; init; } = "root";
        public string Contrasena { get; init; } = "";
        public string BaseDeDatos { get; init; } = "surachih";

        /// <summary>Configuración cargada una sola vez y compartida por todos los repositorios.</summary>
        public static ConfiguracionBD Predeterminada => _predeterminada.Value;
        private static readonly Lazy<ConfiguracionBD> _predeterminada = new(Cargar);

        /// <summary>Cadena de conexión lista para <see cref="MySqlConnection"/>.</summary>
        public string CadenaConexion => new MySqlConnectionStringBuilder
        {
            Server = Servidor,
            Port = Puerto,
            UserID = Usuario,
            Password = Contrasena,
            Database = BaseDeDatos,
            CharacterSet = "utf8mb4",   // acentos y emojis de la BD
            Pooling = true              // reutiliza conexiones: abrir/cerrar por consulta es barato
        }.ConnectionString;

        /// <summary>
        /// Busca <c>dbsettings.local.json</c> subiendo desde la carpeta del ejecutable hasta la raíz
        /// de la solución. Las llaves aceptadas son las del archivo <c>dbsettings.example.json</c>.
        /// </summary>
        public static ConfiguracionBD Cargar()
        {
            string? ruta = BuscarArchivo();
            if (ruta == null) return new ConfiguracionBD();

            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(ruta));
            JsonElement raiz = doc.RootElement;
            var config = new ConfiguracionBD();

            return config with
            {
                Servidor = Leer(raiz, "Server") ?? config.Servidor,
                Puerto = uint.TryParse(Leer(raiz, "Port"), out uint puerto) ? puerto : config.Puerto,
                Usuario = Leer(raiz, "User") ?? config.Usuario,
                Contrasena = Leer(raiz, "Password") ?? config.Contrasena,
                BaseDeDatos = Leer(raiz, "Database") ?? config.BaseDeDatos
            };
        }

        private static string? Leer(JsonElement raiz, string llave)
        {
            if (!raiz.TryGetProperty(llave, out JsonElement valor)) return null;
            return valor.ValueKind == JsonValueKind.Number ? valor.GetRawText() : valor.GetString();
        }

        private static string? BuscarArchivo()
        {
            DirectoryInfo? carpeta = new(AppContext.BaseDirectory);
            while (carpeta != null)
            {
                string candidato = Path.Combine(carpeta.FullName, NombreArchivo);
                if (File.Exists(candidato)) return candidato;
                carpeta = carpeta.Parent;
            }
            return null;
        }
    }
}
