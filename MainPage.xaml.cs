using Microsoft.AspNetCore.Components.WebView;

namespace SuraChihIntegrativeProject
{
    /// <remarks>
    /// Los enlaces externos (boletos, "Cómo llegar"...) los abre BlazorWebView en el navegador del sistema.
    /// </remarks>
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void AlInicializarWebView(object? sender, BlazorWebViewInitializedEventArgs e)
        {
#if WINDOWS
            var opciones = e.WebView.CoreWebView2.Settings;
            opciones.IsZoomControlEnabled = false;
            opciones.IsStatusBarEnabled = false;
#if !DEBUG
            opciones.AreDevToolsEnabled = false;
            opciones.AreDefaultContextMenusEnabled = false;
#endif
#endif
        }
    }
}
