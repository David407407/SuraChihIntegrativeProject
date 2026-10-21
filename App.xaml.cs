namespace SuraChihIntegrativeProject
{
    /// <summary>Aplicación MAUI: crea la única ventana, con el tamaño de diseño (1366 × 820) centrada.</summary>
    public partial class App : Application
    {
        private const double Ancho = 1366, Alto = 820;

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var ventana = new Window(new MainPage())
            {
                Title = "SuraChih",
                Width = Ancho,
                Height = Alto,
                MinimumWidth = 1024,
                MinimumHeight = 700,
            };

            // Centrada en la pantalla principal (las medidas de MAUI son independientes de la escala de Windows).
            var pantalla = DeviceDisplay.Current.MainDisplayInfo;
            if (pantalla.Density > 0)
            {
                ventana.X = Math.Max(0, (pantalla.Width / pantalla.Density - Ancho) / 2);
                ventana.Y = Math.Max(0, (pantalla.Height / pantalla.Density - Alto) / 2);
            }
            return ventana;
        }
    }
}
