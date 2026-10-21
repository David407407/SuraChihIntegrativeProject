using BackendLogica;
using Interfaz;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;

namespace SuraChihIntegrativeProject.Ventanas
{
    /// <summary>
    /// Única ventana de la app. Hospeda la interfaz Blazor (proyecto Interfaz) en un BlazorWebView:
    /// los componentes Razor corren en este mismo proceso y llaman directo a BackendLogica.
    /// </summary>
    /// <remarks>
    /// Los enlaces externos (boletos, "Cómo llegar"...) los abre BlazorWebView en el navegador del sistema.
    /// </remarks>
    public sealed class VentanaPrincipal : Form
    {
        public VentanaPrincipal()
        {
            Text = "SuraChih";
            BackColor = Color.White;
            ClientSize = new Size(1366, 820);
            MinimumSize = new Size(1024, 700);
            StartPosition = FormStartPosition.CenterScreen;

            if (!HayWebView2()) return;

            var servicios = new ServiceCollection();
            servicios.AddWindowsFormsBlazorWebView();
#if DEBUG
            servicios.AddBlazorWebViewDeveloperTools();   // F12 para depurar la interfaz
#endif
            // MySQL o datos de prueba en memoria, según "UseFakeData" en dbsettings.local.json.
            servicios.AgregarInterfaz(FabricaBD.Crear());

            var web = new BlazorWebView
            {
                Dock = DockStyle.Fill,
                HostPage = Path.Combine("wwwroot", "index.html"),
                Services = servicios.BuildServiceProvider()
            };
            web.RootComponents.Add<Rutas>("#app");
            web.RootComponents.Add<HeadOutlet>("head::after");
            web.BlazorWebViewInitializing += (_, e) =>
            {
                // Perfil del navegador en %LocalAppData%\SuraChih (no junto al .exe, que puede ser de solo lectura).
                e.UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SuraChih", "WebView2");
                e.EnvironmentOptions = OpcionesNavegador();
            };
            web.BlazorWebViewInitialized += (_, e) => ConfigurarNavegador(e.WebView.CoreWebView2);
            Controls.Add(web);
        }

        /// <summary>BlazorWebView necesita el runtime de WebView2 (viene con Windows 11 y con Edge).</summary>
        private bool HayWebView2()
        {
            try
            {
                CoreWebView2Environment.GetAvailableBrowserVersionString();
                return true;
            }
            catch (WebView2RuntimeNotFoundException)
            {
                Load += (_, _) =>
                {
                    MessageBox.Show(this,
                        "Falta el componente Microsoft Edge WebView2 Runtime.\nDescárgalo de https://developer.microsoft.com/microsoft-edge/webview2/ e intenta de nuevo.",
                        "SuraChih", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                };
                return false;
            }
        }

        /// <summary>
        /// Colores sobresaturados: en pantallas de gama amplia (muchas laptops con Windows) WebView2 pinta la
        /// página sin convertir sus colores sRGB al perfil del monitor, y todo se ve más intenso que en Figma.
        /// Al abrir una modal Chromium cambia de camino de composición y se ve bien, por eso parecía que solo
        /// "con la modal" estaba correcto. Forzar el perfil sRGB hace que se vea igual en cualquier pantalla.
        /// </summary>
        private static CoreWebView2EnvironmentOptions OpcionesNavegador() => new()
        {
            AdditionalBrowserArguments = "--force-color-profile=srgb"
        };

        private static void ConfigurarNavegador(CoreWebView2 core)
        {
            core.Settings.IsZoomControlEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
#if !DEBUG
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = false;
#endif
        }
    }
}
