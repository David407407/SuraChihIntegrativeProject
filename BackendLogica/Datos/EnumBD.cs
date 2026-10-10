using System.Collections.Concurrent;
using System.Reflection;

namespace BackendLogica.Datos
{
    /// <summary>
    /// Indica el texto con el que un valor de enum se guarda en una columna <c>ENUM</c> de MySQL.
    /// Permite nombrar los enums en español sin cambiar los valores de la BD.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ValorBDAttribute : Attribute
    {
        public string Valor { get; }
        public ValorBDAttribute(string valor) => Valor = valor;
    }

    /// <summary>Convierte entre enums de C# y los textos de las columnas ENUM.</summary>
    public static class EnumBD
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<string, object>> _lectura = new();
        private static readonly ConcurrentDictionary<Type, Dictionary<object, string>> _escritura = new();

        /// <summary>"approved" → <c>EstadoPublicacion.Aprobado</c>.</summary>
        public static T Leer<T>(string valorBD) where T : struct, Enum
        {
            var mapa = _lectura.GetOrAdd(typeof(T), ConstruirLectura);
            return mapa.TryGetValue(valorBD, out object? valor)
                ? (T)valor
                : throw new ArgumentException($"'{valorBD}' no es un valor válido de {typeof(T).Name}.");
        }

        /// <summary><c>EstadoPublicacion.Aprobado</c> → "approved".</summary>
        public static string Escribir(Enum valor)
        {
            var mapa = _escritura.GetOrAdd(valor.GetType(), ConstruirEscritura);
            return mapa[valor];
        }

        private static Dictionary<string, object> ConstruirLectura(Type tipo) =>
            ConstruirEscritura(tipo).ToDictionary(par => par.Value, par => par.Key, StringComparer.OrdinalIgnoreCase);

        private static Dictionary<object, string> ConstruirEscritura(Type tipo) =>
            tipo.GetFields(BindingFlags.Public | BindingFlags.Static).ToDictionary(
                campo => campo.GetValue(null)!,
                campo => campo.GetCustomAttribute<ValorBDAttribute>()?.Valor ?? campo.Name.ToLowerInvariant());
    }
}
