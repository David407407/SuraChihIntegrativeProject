using System.IO;
using System.Windows;

namespace SuraChihIntegrativeProject
{
    /// <summary>Aplicación WPF: prepara WebView2 y abre la única ventana (MainWindow.xaml).</summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            ConfigurarWebView2();
            base.OnStartup(e);
        }

        /// <summary>
        /// WebView2 lee estas variables al crear el navegador de la ventana.
        /// </summary>
        /// <remarks>
        /// <para>Colores sobresaturados: en pantallas de gama amplia (muchas laptops con Windows) WebView2 pinta
        /// la página sin convertir sus colores sRGB al perfil del monitor, y todo se ve más intenso que en Figma.
        /// Forzar el perfil sRGB hace que se vea igual en cualquier pantalla.</para>
        /// <para>El perfil del navegador va en %LocalAppData%\SuraChih (no junto al .exe, que puede ser de solo lectura).</para>
        /// </remarks>
        private static void ConfigurarWebView2()
        {
            const string Argumentos = "WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS";
            string actuales = Environment.GetEnvironmentVariable(Argumentos) ?? "";
            if (!actuales.Contains("--force-color-profile"))
                Environment.SetEnvironmentVariable(Argumentos, $"{actuales} --force-color-profile=srgb".Trim());

            const string CarpetaDatos = "WEBVIEW2_USER_DATA_FOLDER";
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(CarpetaDatos)))
                Environment.SetEnvironmentVariable(CarpetaDatos, Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SuraChih", "WebView2"));
        }
    }
}
