using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace BackendLogica
{
    public class ConexionDB
    {
        public MySqlConnection dbConnection;
        public MySqlCommand dbCommand;

        public ConexionDB() {
            this.CreateConnection();
        }

        public ConexionDB(string query) {
            this.CreateConnection();
            dbCommand = new MySqlCommand(query, dbConnection);
        }

        public bool TestConnection() {
            try
            {
                dbConnection.Open();
                dbConnection.Close();
                return true;
            }   
            catch (Exception er)
            {
                return false;
            }
        }

        private void CreateConnection() {
            string dblocation = "localhost";
            string user = "root";
            string password = "";
            string dbName = "database";

            string connectionString = $"Server={dblocation};Database={dbName};Uid={user};Pwd={password};";

            dbConnection = new MySqlConnection();
            dbConnection.ConnectionString = connectionString;
        }
    
        // Nuevo método genérico para hacer SELECT de cualquier tabla
        public DataTable ObtenerDatos(string tabla, string filtro = "")
        {
            DataTable resultado = new DataTable();
            string query = $"SELECT * FROM {tabla}";

            // Si envías un filtro, lo concatena a la consulta
            if (!string.IsNullOrEmpty(filtro))
            {
                query += $" WHERE {filtro}";
            }

            try
            {
                dbConnection.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, dbConnection))
                {
                    // El DataAdapter ejecuta la consulta y llena el DataTable automáticamente
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        adapter.Fill(resultado);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en la consulta : {ex.Message}");
            }
            finally
            {
                // Siempre aseguramos que la conexión se cierre al terminar
                if (dbConnection.State == ConnectionState.Open)
                {
                    dbConnection.Close();
                }
            }

            return resultado;
        }

        // Método génerico para insertar DATA en tabla Users
        public bool InsertarUsuario(string name, string email, string password, bool is_mod)
        {
            try
            {
                // Asegurarnos de que la conexión esté abierta
                if (dbConnection.State == ConnectionState.Closed)
                {
                    dbConnection.Open();
                }

                // La consulta SQL con parámetros (@) para seguridad
                string query = @"INSERT INTO users (name, email, password, is_mod, registered_date) 
                                VALUES (@name, @email, @password, @is_mod, @registered_date)";

                using (MySqlCommand cmd = new MySqlCommand(query, dbConnection))
                {
                    // Asignar los valores a los parámetros
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@password", password);
                    cmd.Parameters.AddWithValue("@is_mod", is_mod);
                    
                    // Asignamos la fecha y hora exacta del sistema en el momento de la inserción
                    cmd.Parameters.AddWithValue("@registered_date", DateTime.Now);

                    // ExecuteNonQuery ejecuta el INSERT y devuelve el número de filas afectadas
                    int filasAfectadas = cmd.ExecuteNonQuery();
                    
                    // Si insertó al menos una fila, fue exitoso
                    return filasAfectadas > 0;
                }
            }
            catch (Exception ex)
            {
                // En caso de error, lo imprimimos en consola para que sepas qué falló en tu Mac
                Console.WriteLine($"Error al insertar usuario: {ex.Message}");
                return false;
            }
            finally
            {
                // Cerramos la conexión para no saturar la base de datos
                if (dbConnection.State == ConnectionState.Open)
                {
                    dbConnection.Close();
                }
            }
        }

        // Método para actualizar un registro en la tabla Users
        public bool ActualizarUsuario(int id, string name, string email, string password, bool is_mod)
        {
            try
            {
                if (dbConnection.State == ConnectionState.Closed)
                {
                    dbConnection.Open();
                }

                // La consulta de UPDATE. No actualizamos registered_date porque esa fecha no suele cambiar.
                string query = @"UPDATE users 
                                SET name = @name, email = @email, password = @password, is_mod = @is_mod 
                                WHERE id_user = @id";

                using (MySqlCommand cmd = new MySqlCommand(query, dbConnection))
                {
                    // Asignamos los valores, incluyendo el ID indispensable para el WHERE
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@password", password);
                    cmd.Parameters.AddWithValue("@is_mod", is_mod);

                    // ExecuteNonQuery devuelve el número de filas modificadas
                    int filasAfectadas = cmd.ExecuteNonQuery();
                    
                    // Si modificó al menos una fila (es decir, encontró el ID), retorna true
                    return filasAfectadas > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar usuario: {ex.Message}");
                return false;
            }
            finally
            {
                if (dbConnection.State == ConnectionState.Open)
                {
                    dbConnection.Close();
                }
            }
        }

        // Función para eliminar usuarios
        public bool EliminarUsuario(int id)
        {
            try
            {
                // Verificamos y abrimos la conexión
                if (dbConnection.State == ConnectionState.Closed)
                {
                    dbConnection.Open();
                }

                // Consulta DELETE con parámetro para el ID
                string query = "DELETE FROM users WHERE id_user = @id";

                using (MySqlCommand cmd = new MySqlCommand(query, dbConnection))
                {
                    // Pasamos el parámetro de forma segura
                    cmd.Parameters.AddWithValue("@id", id);

                    // Ejecutamos la consulta y obtenemos el número de filas eliminadas
                    int filasAfectadas = cmd.ExecuteNonQuery();
                    
                    // Retorna true si logró eliminar al menos una fila (el ID existía)
                    return filasAfectadas > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar usuario: {ex.Message}");
                return false;
            }
            finally
            {
                // Nos aseguramos de cerrar la conexión
                if (dbConnection.State == ConnectionState.Open)
                {
                    dbConnection.Close();
                }
            }
        }

    }
}
