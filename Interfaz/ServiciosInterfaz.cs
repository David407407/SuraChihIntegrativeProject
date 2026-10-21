using BackendLogica.Contratos;
using Interfaz.Estado;
using Interfaz.Servicios;
using Microsoft.Extensions.DependencyInjection;

namespace Interfaz
{
    public static class ServiciosInterfaz
    {
        /// <summary>
        /// Registra todo lo que usan los componentes. Lo llaman los dos anfitriones (la ventana de
        /// escritorio y el servidor de prueba del navegador).
        /// </summary>
        /// <param name="bd">MySQL o los datos de prueba en memoria (<c>BackendLogica.FabricaBD.Crear()</c>).</param>
        /// <remarks>
        /// Todo es Scoped: en la app de escritorio vive lo que vive la ventana; en el navegador, lo que
        /// vive la pestaña (al recargarla se pierde la sesión).
        /// </remarks>
        public static IServiceCollection AgregarInterfaz(this IServiceCollection servicios, ISuraChihBD bd)
        {
            servicios.AddSingleton(bd);

            servicios.AddScoped<Sesion>();
            servicios.AddScoped<CorreoSimulado>();
            servicios.AddScoped<ServicioLanding>();
            servicios.AddScoped<ServicioHome>();
            servicios.AddScoped<ServicioEvento>();
            servicios.AddScoped<ServicioLugar>();
            servicios.AddScoped<ServicioCuenta>();
            servicios.AddScoped<ServicioPerfil>();
            servicios.AddScoped<ServicioFavoritos>();

            servicios.AddScoped<InteropJs>();
            servicios.AddScoped<Avisos>();
            servicios.AddScoped<Modales>();
            servicios.AddScoped<Hojas>();
            servicios.AddScoped<Acciones>();
            return servicios;
        }
    }
}
