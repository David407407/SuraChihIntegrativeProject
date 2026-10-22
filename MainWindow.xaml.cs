using System.Windows;
using BackendLogica;
using Interfaz;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.DependencyInjection;

namespace SuraChihIntegrativeProject
{
    /// <summary>
    /// Registra BlazorWebView y los servicios de la interfaz para la ventana.
    /// </summary>
    /// <remarks>
    /// Los enlaces externos (boletos, "Cómo llegar"...) los abre BlazorWebView en el navegador del sistema.
    /// </remarks>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            var servicios = new ServiceCollection();
            servicios.AddWpfBlazorWebView();
#if DEBUG
            servicios.AddBlazorWebViewDeveloperTools();   // F12 para depurar la interfaz
#endif
            // MySQL o datos de prueba en memoria, según "UseFakeData" en dbsettings.local.json.
            servicios.AgregarInterfaz(FabricaBD.Crear());
            Resources.Add("servicios", servicios.BuildServiceProvider());

            InitializeComponent();
        }

        private void AlInicializarWebView(object? sender, BlazorWebViewInitializedEventArgs e)
        {
            var opciones = e.WebView.CoreWebView2.Settings;
            opciones.IsZoomControlEnabled = false;
            opciones.IsStatusBarEnabled = false;
#if !DEBUG
            opciones.AreDevToolsEnabled = false;
            opciones.AreDefaultContextMenusEnabled = false;
#endif
        }
    }
}
