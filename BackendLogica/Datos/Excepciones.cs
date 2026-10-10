using MySql.Data.MySqlClient;

namespace BackendLogica.Datos
{
    /// <summary>
    /// Error base de la librería. El frontend solo necesita atrapar este tipo:
    /// <see cref="Exception.Message"/> siempre es un texto que se puede mostrar al usuario.
    /// </summary>
    public class SuraChihException : Exception
    {
        public SuraChihException(string mensaje, Exception? causa = null) : base(mensaje, causa) { }
    }

    /// <summary>
    /// Una regla de negocio de la BD rechazó la operación (triggers con <c>SIGNAL SQLSTATE '45000'</c>).
    /// Ej.: "Solo organizadores aprobados pueden publicar eventos."
    /// </summary>
    public sealed class ReglaNegocioException : SuraChihException
    {
        public ReglaNegocioException(string mensaje, Exception? causa = null) : base(mensaje, causa) { }
    }

    /// <summary>Se intentó registrar algo que ya existe (usuario, correo, reseña repetida...).</summary>
    public sealed class DuplicadoException : SuraChihException
    {
        public DuplicadoException(string mensaje, Exception? causa = null) : base(mensaje, causa) { }
    }

    /// <summary>Los datos no cumplen una restricción CHECK o una validación previa.</summary>
    public sealed class DatosInvalidosException : SuraChihException
    {
        public DatosInvalidosException(string mensaje, Exception? causa = null) : base(mensaje, causa) { }
    }

    /// <summary>
    /// El diseño lo pide pero la BD v1 todavía no lo puede guardar (favoritos de lugares, fecha o precio
    /// por confirmar, borradores sin ubicación). Solo lo lanza el repositorio real; el falso sí lo soporta.
    /// </summary>
    public sealed class PendienteBDException : SuraChihException
    {
        public PendienteBDException(string funcion)
            : base($"{funcion} todavía no está disponible: falta actualizar la base de datos.") { }
    }

    /// <summary>No hay conexión con el servidor MySQL.</summary>
    public sealed class SinConexionException : SuraChihException
    {
        public SinConexionException(Exception causa)
            : base("No se pudo conectar con la base de datos. Revisa que MySQL esté encendido y tu dbsettings.local.json.", causa) { }
    }

    /// <summary>Traduce los códigos de error de MySQL a excepciones de la librería.</summary>
    /// <remarks>
    /// Convención del equipo: <c>MySqlException.Number == 1644</c> (SIGNAL de un trigger) se convierte en
    /// <see cref="ReglaNegocioException"/> con el mensaje del trigger TAL CUAL, para mostrarlo sin reescribirlo.
    /// El repositorio falso lanza la misma excepción con los mismos textos (ver <c>Falso/MensajesTrigger</c>).
    /// </remarks>
    internal static class TraductorErrores
    {
        // https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html
        internal const int SignalPersonalizado = 1644;  // SIGNAL SQLSTATE '45000' de los triggers
        private const int LlaveDuplicada = 1062;
        private const int CheckViolado = 3819;
        private const int LlaveForaneaHijo = 1452;
        private const int SinServidor = 1042;
        private const int AccesoDenegado = 1045;
        private const int BaseDesconocida = 1049;

        public static SuraChihException Traducir(MySqlException ex) => ex.Number switch
        {
            SignalPersonalizado => new ReglaNegocioException(ex.Message, ex),
            LlaveDuplicada => new DuplicadoException(MensajeDuplicado(ex.Message), ex),
            CheckViolado => new DatosInvalidosException("Algún dato no tiene el formato correcto.", ex),
            LlaveForaneaHijo => new DatosInvalidosException("El registro relacionado no existe.", ex),
            SinServidor or AccesoDenegado or BaseDesconocida or 0 => new SinConexionException(ex),
            _ => new SuraChihException("Ocurrió un error con la base de datos.", ex)
        };

        // El mensaje de MySQL trae el nombre de la restricción: "... for key 'user.uq_user_email'"
        private static string MensajeDuplicado(string mensajeMySql)
        {
            if (mensajeMySql.Contains("uq_user_email")) return "Ese correo ya está registrado.";
            if (mensajeMySql.Contains("uq_user_username")) return "Ese nombre de usuario ya está ocupado.";
            if (mensajeMySql.Contains("uq_evrev_once") || mensajeMySql.Contains("uq_plrev_once")) return "Ya dejaste una reseña aquí.";
            if (mensajeMySql.Contains("PRIMARY") && mensajeMySql.Contains("organizer_profile")) return "Ya enviaste una solicitud de organizador.";
            return "Ese registro ya existe.";
        }
    }
}
