using System.Data.Common;

namespace BackendLogica.Datos
{
    /// <summary>
    /// Lecturas tipadas por nombre de columna para mapear filas a modelos sin repetir
    /// <c>Convert</c> y <c>IsDBNull</c> en cada repositorio.
    /// </summary>
    /// <example><code>new Etiqueta(r.Entero("id"), r.Texto("name"), ...)</code></example>
    public static class LectorExtensiones
    {
        public static bool EsNulo(this DbDataReader r, string columna) => r.IsDBNull(r.GetOrdinal(columna));

        public static string Texto(this DbDataReader r, string columna) => Convert.ToString(r[columna])!;
        public static string? TextoONulo(this DbDataReader r, string columna) => r.EsNulo(columna) ? null : r.Texto(columna);

        public static int Entero(this DbDataReader r, string columna) => Convert.ToInt32(r[columna]);
        public static int? EnteroONulo(this DbDataReader r, string columna) => r.EsNulo(columna) ? null : r.Entero(columna);
        public static long EnteroLargo(this DbDataReader r, string columna) => Convert.ToInt64(r[columna]);

        public static decimal Decimal(this DbDataReader r, string columna) => Convert.ToDecimal(r[columna]);
        public static decimal? DecimalONulo(this DbDataReader r, string columna) => r.EsNulo(columna) ? null : r.Decimal(columna);

        /// <summary>Acepta TINYINT/BOOLEAN y también expresiones como <c>(a = b)</c> que MySQL devuelve como BIGINT.</summary>
        public static bool Bool(this DbDataReader r, string columna) => Convert.ToInt64(r[columna]) != 0;

        public static DateTime Fecha(this DbDataReader r, string columna) => Convert.ToDateTime(r[columna]);
        public static DateTime? FechaONula(this DbDataReader r, string columna) => r.EsNulo(columna) ? null : r.Fecha(columna);

        /// <summary>Columna TIME de MySQL.</summary>
        public static TimeSpan Hora(this DbDataReader r, string columna) => (TimeSpan)r[columna];
        public static TimeSpan? HoraONula(this DbDataReader r, string columna) => r.EsNulo(columna) ? null : r.Hora(columna);

        /// <summary>Columna ENUM de MySQL mapeada a un enum de C# (ver <see cref="ValorBDAttribute"/>).</summary>
        public static T Enum<T>(this DbDataReader r, string columna) where T : struct, System.Enum => EnumBD.Leer<T>(r.Texto(columna));
    }
}
