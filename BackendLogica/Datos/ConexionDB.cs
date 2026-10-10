using System.Collections;
using System.Data.Common;
using System.Text.RegularExpressions;
using BackendLogica.Configuracion;
using MySql.Data.MySqlClient;

namespace BackendLogica.Datos
{
    /// <summary>
    /// Clase padre de todos los repositorios (<c>UsuarioDB</c>, <c>EventoDB</c>, <c>LugarDB</c>...).
    /// Concentra lo que antes se repetía en cada método: abrir y cerrar la conexión, crear el comando,
    /// pasar parámetros de forma segura, leer resultados y traducir los errores de MySQL.
    /// </summary>
    /// <remarks>
    /// <para>Cada operación abre su propia conexión y la cierra al terminar. MySQL las reutiliza
    /// internamente (pooling), así que es seguro usar el mismo repositorio desde varios hilos.</para>
    /// <para>Los parámetros se pasan con un objeto anónimo: <c>new { id = 5 }</c> llena <c>@id</c>.
    /// Si un valor es una lista (<c>new { ids = new[] { 1, 2 } }</c>), <c>@ids</c> se expande a
    /// <c>(@ids0, @ids1)</c> para usarlo con <c>IN @ids</c>. Los enums se convierten con <see cref="EnumBD"/>.</para>
    /// </remarks>
    public abstract class ConexionDB
    {
        /// <summary>Configuración con la que este repositorio se conecta.</summary>
        protected ConfiguracionBD Configuracion { get; }

        /// <param name="configuracion">Opcional: por defecto se usa <see cref="ConfiguracionBD.Predeterminada"/>.</param>
        protected ConexionDB(ConfiguracionBD? configuracion = null)
        {
            Configuracion = configuracion ?? ConfiguracionBD.Predeterminada;
        }

        /// <summary>Intenta abrir una conexión. Útil para mostrar un aviso al iniciar la app.</summary>
        public async Task<bool> ProbarConexionAsync(CancellationToken ct = default)
        {
            try
            {
                await using var conexion = new MySqlConnection(Configuracion.CadenaConexion);
                await conexion.OpenAsync(ct);
                return true;
            }
            catch (MySqlException)
            {
                return false;
            }
        }

        // ------------------------------------------------------------------
        // Operaciones para las clases hijas
        // ------------------------------------------------------------------

        /// <summary>Ejecuta un SELECT y convierte cada fila con <paramref name="mapear"/>.</summary>
        protected Task<List<T>> ConsultarAsync<T>(string sql, Func<DbDataReader, T> mapear, object? parametros = null, CancellationToken ct = default)
            => ConConexionAsync(c => Comandos.ConsultarAsync(c, null, sql, mapear, parametros, ct), ct);

        /// <summary>Ejecuta un SELECT y devuelve la primera fila, o <c>default</c> si no hubo resultados.</summary>
        protected async Task<T?> ConsultarUnoAsync<T>(string sql, Func<DbDataReader, T> mapear, object? parametros = null, CancellationToken ct = default)
            => (await ConsultarAsync(sql, mapear, parametros, ct)).FirstOrDefault();

        /// <summary>Devuelve el primer valor de la primera fila (COUNT, EXISTS, un id...).</summary>
        protected Task<T?> EscalarAsync<T>(string sql, object? parametros = null, CancellationToken ct = default)
            => ConConexionAsync(c => Comandos.EscalarAsync<T>(c, null, sql, parametros, ct), ct);

        /// <summary>INSERT/UPDATE/DELETE. Devuelve el número de filas afectadas.</summary>
        protected Task<int> EjecutarAsync(string sql, object? parametros = null, CancellationToken ct = default)
            => ConConexionAsync(c => Comandos.EjecutarAsync(c, null, sql, parametros, ct), ct);

        /// <summary>INSERT en una tabla con AUTO_INCREMENT. Devuelve el id generado.</summary>
        protected Task<int> InsertarAsync(string sql, object? parametros = null, CancellationToken ct = default)
            => ConConexionAsync(c => Comandos.InsertarAsync(c, null, sql, parametros, ct), ct);

        /// <summary>
        /// Ejecuta varias operaciones como una sola: si alguna falla, ninguna se guarda.
        /// Dentro de <paramref name="trabajo"/> usa los métodos de <see cref="Transaccion"/>.
        /// </summary>
        protected Task<T> EnTransaccionAsync<T>(Func<Transaccion, Task<T>> trabajo, CancellationToken ct = default)
            => ConConexionAsync(async conexion =>
            {
                await using var tx = await conexion.BeginTransactionAsync(ct);
                try
                {
                    T resultado = await trabajo(new Transaccion(conexion, tx, ct));
                    await tx.CommitAsync(ct);
                    return resultado;
                }
                catch
                {
                    await tx.RollbackAsync(CancellationToken.None);
                    throw;
                }
            }, ct);

        /// <summary>Abre la conexión, ejecuta el trabajo, la cierra y traduce los errores de MySQL.</summary>
        private async Task<T> ConConexionAsync<T>(Func<MySqlConnection, Task<T>> trabajo, CancellationToken ct)
        {
            try
            {
                await using var conexion = new MySqlConnection(Configuracion.CadenaConexion);
                await conexion.OpenAsync(ct);
                return await trabajo(conexion);
            }
            catch (MySqlException ex)
            {
                throw TraductorErrores.Traducir(ex);
            }
        }

        // ------------------------------------------------------------------
        // Transacción: mismas operaciones, pero sobre una conexión compartida
        // ------------------------------------------------------------------

        /// <summary>Operaciones disponibles dentro de <see cref="EnTransaccionAsync{T}"/>.</summary>
        protected sealed class Transaccion
        {
            private readonly MySqlConnection _conexion;
            private readonly MySqlTransaction _tx;
            private readonly CancellationToken _ct;

            internal Transaccion(MySqlConnection conexion, MySqlTransaction tx, CancellationToken ct)
            {
                _conexion = conexion;
                _tx = tx;
                _ct = ct;
            }

            public Task<List<T>> ConsultarAsync<T>(string sql, Func<DbDataReader, T> mapear, object? parametros = null)
                => Comandos.ConsultarAsync(_conexion, _tx, sql, mapear, parametros, _ct);

            public Task<T?> EscalarAsync<T>(string sql, object? parametros = null)
                => Comandos.EscalarAsync<T>(_conexion, _tx, sql, parametros, _ct);

            public Task<int> EjecutarAsync(string sql, object? parametros = null)
                => Comandos.EjecutarAsync(_conexion, _tx, sql, parametros, _ct);

            public Task<int> InsertarAsync(string sql, object? parametros = null)
                => Comandos.InsertarAsync(_conexion, _tx, sql, parametros, _ct);
        }

        // ------------------------------------------------------------------
        // Implementación compartida (con o sin transacción)
        // ------------------------------------------------------------------

        private static class Comandos
        {
            public static async Task<List<T>> ConsultarAsync<T>(MySqlConnection c, MySqlTransaction? tx, string sql,
                Func<DbDataReader, T> mapear, object? parametros, CancellationToken ct)
            {
                await using var cmd = Crear(c, tx, sql, parametros);
                await using DbDataReader lector = await cmd.ExecuteReaderAsync(ct);
                var filas = new List<T>();
                while (await lector.ReadAsync(ct)) filas.Add(mapear(lector));
                return filas;
            }

            public static async Task<T?> EscalarAsync<T>(MySqlConnection c, MySqlTransaction? tx, string sql, object? parametros, CancellationToken ct)
            {
                await using var cmd = Crear(c, tx, sql, parametros);
                object? valor = await cmd.ExecuteScalarAsync(ct);
                if (valor == null || valor is DBNull) return default;
                Type destino = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                return (T)Convert.ChangeType(valor, destino);
            }

            public static async Task<int> EjecutarAsync(MySqlConnection c, MySqlTransaction? tx, string sql, object? parametros, CancellationToken ct)
            {
                await using var cmd = Crear(c, tx, sql, parametros);
                return await cmd.ExecuteNonQueryAsync(ct);
            }

            public static async Task<int> InsertarAsync(MySqlConnection c, MySqlTransaction? tx, string sql, object? parametros, CancellationToken ct)
            {
                await using var cmd = Crear(c, tx, sql, parametros);
                await cmd.ExecuteNonQueryAsync(ct);
                return (int)cmd.LastInsertedId;
            }

            private static MySqlCommand Crear(MySqlConnection c, MySqlTransaction? tx, string sql, object? parametros)
            {
                var cmd = new MySqlCommand { Connection = c, Transaction = tx };
                if (parametros != null)
                {
                    foreach (var propiedad in parametros.GetType().GetProperties())
                    {
                        string nombre = "@" + propiedad.Name;
                        object? valor = propiedad.GetValue(parametros);

                        if (valor is IEnumerable lista and not string and not byte[])
                            sql = ExpandirLista(sql, cmd, nombre, lista);
                        else
                            cmd.Parameters.AddWithValue(nombre, Normalizar(valor));
                    }
                }
                cmd.CommandText = sql;
                return cmd;
            }

            // IN @ids  →  IN (@ids0, @ids1, ...). Una lista vacía se vuelve (NULL) para que no coincida nada.
            private static string ExpandirLista(string sql, MySqlCommand cmd, string nombre, IEnumerable lista)
            {
                var nombres = new List<string>();
                int i = 0;
                foreach (object? elemento in lista)
                {
                    string nombreElemento = nombre + i++;
                    nombres.Add(nombreElemento);
                    cmd.Parameters.AddWithValue(nombreElemento, Normalizar(elemento));
                }
                string reemplazo = nombres.Count == 0 ? "(NULL)" : "(" + string.Join(", ", nombres) + ")";
                return Regex.Replace(sql, Regex.Escape(nombre) + @"\b", reemplazo);
            }

            private static object Normalizar(object? valor) => valor switch
            {
                null => DBNull.Value,
                Enum e => EnumBD.Escribir(e),
                _ => valor
            };
        }
    }
}
