using BackendLogica.Configuracion;
using BackendLogica.Contratos;
using BackendLogica.Falso;

namespace BackendLogica
{
    /// <summary>
    /// Elige el origen de datos. Así las pantallas no saben si hablan con MySQL o con la memoria:
    /// <code>
    /// ISuraChihBD bd = FabricaBD.Crear();
    /// </code>
    /// Con <c>"UseFakeData": true</c> en <c>dbsettings.local.json</c> se trabaja con los datos de prueba
    /// en memoria, sin instalar MySQL.
    /// </summary>
    public static class FabricaBD
    {
        public static ISuraChihBD Crear(ConfiguracionBD? configuracion = null)
        {
            configuracion ??= ConfiguracionBD.Predeterminada;
            return configuracion.UsarDatosFalsos ? new SuraChihFalso() : new SuraChihBD(configuracion);
        }
    }
}
