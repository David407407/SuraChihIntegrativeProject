using BackendLogica.Contratos;
using Interfaz.Estado;
using Interfaz.Servicios;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Interfaz
{
    public static class ServiciosInterfaz
    {
        /// <summary>
        /// Registra todo lo que usan los componentes. Lo llama la ventana de escritorio (MainWindow.xaml.cs).
        /// </summary>
        /// <param name="bd">MySQL o los datos de prueba en memoria (<c>BackendLogica.FabricaBD.Crear()</c>).</param>
        /// <remarks>Todo es Scoped: vive lo que vive la ventana.</remarks>
        public static IServiceCollection AgregarInterfaz(this IServiceCollection servicios, ISuraChihBD bd)
        {
            servicios.AddSingleton(bd);
            servicios.AddFluentUIComponents();

            servicios.AddScoped<Sesion>();
            servicios.AddScoped<CorreoSimulado>();
            servicios.AddScoped<ServicioLanding>();
            servicios.AddScoped<ServicioHome>();
            servicios.AddScoped<ServicioEvento>();
            servicios.AddScoped<ServicioLugar>();
            servicios.AddScoped<ServicioGoogle>();
            servicios.AddScoped<ServicioCuenta>();
            servicios.AddScoped<ServicioPerfil>();
            servicios.AddScoped<ServicioGustos>();
            servicios.AddScoped<ServicioFavoritos>();
            servicios.AddScoped<ServicioPlanes>();
            servicios.AddScoped<ServicioResenas>();
            servicios.AddScoped<ServicioModeracion>();
            servicios.AddScoped<ServicioReportes>();
            servicios.AddScoped<ServicioOrganizador>();
            servicios.AddScoped<ServicioPanelOrganizador>();
            servicios.AddScoped<ServicioBusqueda>();
            servicios.AddScoped<ServicioPublicacion>();
            servicios.AddScoped<ServicioRegistroLugar>();
            servicios.AddScoped<ServicioImagenes>();

            servicios.AddScoped<InteropJs>();
            servicios.AddScoped<Avisos>();
            servicios.AddScoped<Modales>();
            servicios.AddScoped<Hojas>();
            servicios.AddScoped<Acciones>();
            return servicios;
        }
    }
}
