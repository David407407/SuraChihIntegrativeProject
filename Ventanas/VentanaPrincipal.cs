using System.Diagnostics;
using BackendLogica;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using SuraChihIntegrativeProject.Controladores;
using SuraChihIntegrativeProject.Puente;
using SuraChihIntegrativeProject.Servicios;

namespace SuraChihIntegrativeProject.Ventanas
{
    /// <summary>
    /// Única ventana de la app. Hospeda la interfaz web de <c>wwwroot</c> en un WebView2 y conecta
    /// el puente de mensajes con los controladores, que a su vez usan BackendLogica.
    /// </summary>
    public sealed class VentanaPrincipal : Form
    {
        /// <summary>Dominio virtual con el que WebView2 sirve la carpeta wwwroot (no sale a internet).</summary>
        private const string HostApp = "app.surachih";
        private static readonly string UrlInicio = $"https://{HostApp}/index.html";

        private readonly WebView2 _web = new() { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.White };
        /// <summary>MySQL o datos de prueba en memoria, según "UseFakeData" en dbsettings.local.json.</summary>
        private readonly BackendLogica.Contratos.ISuraChihBD _bd = FabricaBD.Crear();
        private readonly Sesion _sesion = new();

        public VentanaPrincipal()
        {
            Text = "SuraChih";
            BackColor = Color.White;
            ClientSize = new Size(1366, 820);
            MinimumSize = new Size(1024, 700);
            StartPosition = FormStartPosition.CenterScreen;
            Controls.Add(_web);
            Load += async (_, _) => await InicializarAsync();
        }

        private async Task InicializarAsync()
        {
            try
            {
                // Perfil del navegador en %LocalAppData%\SuraChih (no junto al .exe, que puede ser de solo lectura).
                string datosUsuario = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SuraChih", "WebView2");
                var entorno = await CoreWebView2Environment.CreateAsync(userDataFolder: datosUsuario);
                await _web.EnsureCoreWebView2Async(entorno);
            }
            catch (WebView2RuntimeNotFoundException)
            {
                MessageBox.Show(this,
                    "Falta el componente Microsoft Edge WebView2 Runtime.\nDescárgalo de https://developer.microsoft.com/microsoft-edge/webview2/ e intenta de nuevo.",
                    "SuraChih", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            CoreWebView2 core = _web.CoreWebView2;
            ConfigurarNavegador(core);

            var puente = new PuenteWeb(core);
            new ControladorLanding(_bd, _sesion).Registrar(puente);

            core.Navigate(UrlInicio);
        }

        private static void ConfigurarNavegador(CoreWebView2 core)
        {
            string carpetaWeb = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            core.SetVirtualHostNameToFolderMapping(HostApp, carpetaWeb, CoreWebView2HostResourceAccessKind.Allow);

            core.Settings.IsZoomControlEnabled = false;
            core.Settings.IsStatusBarEnabled = false;
#if DEBUG
            core.Settings.AreDevToolsEnabled = true;          // F12 para depurar la interfaz
#else
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = false;
#endif

            // Los enlaces externos (boletos, redes sociales...) se abren en el navegador del sistema.
            core.NewWindowRequested += (_, e) =>
            {
                e.Handled = true;
                AbrirExterno(e.Uri);
            };
            core.NavigationStarting += (_, e) =>
            {
                if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var destino) && destino.Host != HostApp)
                {
                    e.Cancel = true;
                    AbrirExterno(e.Uri);
                }
            };
        }

        private static void AbrirExterno(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
    }
}
