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

        /// <summary>
        /// Llave <c>"UseFakeData": true</c>: <see cref="FabricaBD.Crear"/> devuelve el repositorio falso
        /// en memoria (datos de prueba de surachih_v1.sql) y no se toca MySQL.
        /// </summary>
        public bool UsarDatosFalsos { get; init; }

        /// <summary>
        /// Llave <c>"CloudinaryCloudName"</c>: cuenta de Cloudinary a la que "Publicar evento" sube las fotos.
        /// Sin ella (y sin <see cref="CloudinaryPreset"/>) no se pueden subir fotos nuevas.
        /// </summary>
        public string? CloudinaryNube { get; init; }

        /// <summary>
        /// Llave <c>"CloudinaryUploadPreset"</c>: upload preset <b>unsigned</b> de esa cuenta (Settings → Upload).
        /// No es una contraseña: solo permite subir imágenes a la carpeta que diga el preset.
        /// </summary>
        public string? CloudinaryPreset { get; init; }

        /// <summary>
        /// Llaves <c>"GoogleClientId"</c> y <c>"GoogleClientSecret"</c>: cliente OAuth de tipo "App de escritorio"
        /// de Google Cloud (Google Auth Platform → Clientes) para "Continuar con Google". Nunca van en git.
        /// </summary>
        public string? GoogleClientId { get; init; }
        public string? GoogleClientSecret { get; init; }

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
            SslMode = Servidor is "localhost" or "127.0.0.1"
                ? MySqlSslMode.Preferred   // MySQL local: cifra si se puede
                : MySqlSslMode.Required,   // BD compartida en Aiven: siempre cifrado
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
                BaseDeDatos = Leer(raiz, "Database") ?? config.BaseDeDatos,
                UsarDatosFalsos = raiz.TryGetProperty("UseFakeData", out JsonElement falso) && falso.ValueKind == JsonValueKind.True,
                CloudinaryNube = Leer(raiz, "CloudinaryCloudName"),
                CloudinaryPreset = Leer(raiz, "CloudinaryUploadPreset"),
                GoogleClientId = Leer(raiz, "GoogleClientId"),
                GoogleClientSecret = Leer(raiz, "GoogleClientSecret")
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
