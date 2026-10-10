using SuraChihIntegrativeProject.Ventanas;

namespace SuraChihIntegrativeProject
{
    internal static class Program
    {
        /// <summary>Punto de entrada: abre la ventana principal que hospeda la interfaz web.</summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new VentanaPrincipal());
        }
    }
}
