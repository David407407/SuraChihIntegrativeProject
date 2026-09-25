using System;
using System.Data;
using BackendLogica;
using MySql.Data.MySqlClient;

ConexionDB db = new ConexionDB();
Console.WriteLine("Iniciando prueba de conexión...");

if (db.TestConnection())
{
    Console.WriteLine("¡Conectado! Extrayendo datos...");
    
    bool exito = db.EliminarUsuario(2);

    // Prueba 1: Traer toda la tabla (Reemplaza "tu_tabla" con una real)
    DataTable usersData = db.ObtenerDatos("users");
    
    // Prueba 2: Traer con filtro (Opcional)
    // DataTable datosFiltrados = db.ObtenerDatos("tu_tabla", "id = 1");

    Console.WriteLine("\n--- DATOS DE LA TABLA ---");

    foreach (DataColumn columna in usersData.Columns)
    {
        // El "-20" asegura que cada columna ocupe exactamente 20 caracteres alineados a la izquierda
        Console.Write($"{columna.ColumnName,-20}"); 
    }
    Console.WriteLine(); 

    Console.WriteLine(new string('-', usersData.Columns.Count * 20));

    foreach (DataRow fila in usersData.Rows)
    {
        foreach (var celda in fila.ItemArray)
        {
            Console.Write($"{celda.ToString(),-20}");
        }
        Console.WriteLine(); 
    }
}
else
{
    Console.WriteLine("Fallo en la conexión.");
}
