using System.Drawing.Text;
using FontAwesome.Sharp;

namespace SuraChihIntegrativeProject
{
    // Colores, tipografías e iconos compartidos por todas las pantallas
    public static class Theme
    {
        public static readonly Color Blanco = Color.FromArgb(0xF9, 0xFA, 0xFB);
        public static readonly Color Verde = Color.FromArgb(0x3E, 0x81, 0x6A);
        public static readonly Color Negro = Color.FromArgb(0x18, 0x18, 0x1B);
        public static readonly Color Gris = Color.FromArgb(0xCC, 0xCC, 0xCC);
        // Gris oscuro derivado del negro, para texto secundario legible sobre blanco
        public static readonly Color TextoSecundario = Color.FromArgb(0x6B, 0x6B, 0x72);

        private static readonly PrivateFontCollection Privadas = CargarFuentesLocales();

        // Si pones los .ttf en la carpeta Fonts (se copia al compilar), se usan aunque no estén instaladas
        private static PrivateFontCollection CargarFuentesLocales()
        {
            var coleccion = new PrivateFontCollection();
            string carpeta = Path.Combine(AppContext.BaseDirectory, "Fonts");
            if (Directory.Exists(carpeta))
                foreach (string ttf in Directory.GetFiles(carpeta, "*.ttf"))
                    coleccion.AddFontFile(ttf);
            return coleccion;
        }

        private static FontFamily? Buscar(string nombre)
        {
            foreach (var f in Privadas.Families)
                if (f.Name.Equals(nombre, StringComparison.OrdinalIgnoreCase)) return f;
            try { return new FontFamily(nombre); } catch (ArgumentException) { return null; }
        }

        private static Font Crear(string nombre, float tamaño, FontStyle estilo)
        {
            var familia = Buscar(nombre);
            if (familia == null) return new Font("Segoe UI", tamaño, estilo);
            if (!familia.IsStyleAvailable(estilo)) estilo = FontStyle.Regular;
            return new Font(familia, tamaño, estilo, GraphicsUnit.Point);
        }

        // Títulos y subtítulos
        public static Font Titulo(float tamaño, FontStyle estilo = FontStyle.Bold) => Crear("Montserrat", tamaño, estilo);

        // Todo lo demás
        public static Font Cuerpo(float tamaño, FontStyle estilo = FontStyle.Regular) => Crear("Plus Jakarta Sans", tamaño, estilo);

        public static Bitmap Icono(IconChar icono, int tamaño, Color color, IconFont fuente = IconFont.Solid)
            => icono.ToBitmap(fuente, tamaño, color);
    }
}
