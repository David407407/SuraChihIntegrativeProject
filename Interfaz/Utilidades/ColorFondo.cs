namespace Interfaz.Utilidades
{
    /// <summary>
    /// Fondo del carrusel del Home a partir del color que más se ve en la foto (<c>colorDeImagen</c> de
    /// interop.js): un degradado de ese color a un tono vecino más oscuro. El texto del carrusel es blanco,
    /// así que los dos colores se oscurecen hasta tener contraste de 4.5:1 o más (WCAG AA).
    /// </summary>
    public static class ColorFondo
    {
        /// <summary>Luminancia máxima para que el blanco contraste 4.5:1: (1 + 0.05) / (L + 0.05) ≥ 4.5.</summary>
        private const double LuminanciaMaxima = 0.18;

        /// <param name="rgb">[r, g, b] de 0 a 255.</param>
        /// <returns><c>linear-gradient(...)</c> listo para <c>background</c>, o null si el color no sirve.</returns>
        public static string? Degradado(IReadOnlyList<int>? rgb)
        {
            if (rgb is not { Count: 3 }) return null;
            var (h, s, l) = AHsl(rgb[0], rgb[1], rgb[2]);

            // Ni pastel ni chillón ni casi negro: la saturación se acota, la luz queda entre 26 % y 42 %, y
            // luego baja lo que haga falta para que el blanco se lea.
            s = s < 0.08 ? s : Math.Clamp(s, 0.35, 0.75);
            l = Math.Clamp(l, 0.26, 0.42);
            string inicio = Css(Legible(h, s, l));
            string fin = Css(Legible((h + 18) % 360, s, l * 0.7));
            return $"linear-gradient(120deg, {inicio} 0%, {fin} 100%)";
        }

        /// <summary>Baja la luz (HSL) hasta que el color tenga contraste suficiente con el blanco.</summary>
        private static (int R, int G, int B) Legible(double h, double s, double l)
        {
            var color = ARgb(h, s, l);
            while (Luminancia(color) > LuminanciaMaxima && l > 0.05)
            {
                l -= 0.02;
                color = ARgb(h, s, l);
            }
            return color;
        }

        private static string Css((int R, int G, int B) c) => $"rgb({c.R}, {c.G}, {c.B})";

        /// <summary>Luminancia relativa de WCAG (0 = negro, 1 = blanco).</summary>
        private static double Luminancia((int R, int G, int B) c)
        {
            static double Canal(int v)
            {
                double x = v / 255.0;
                return x <= 0.03928 ? x / 12.92 : Math.Pow((x + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * Canal(c.R) + 0.7152 * Canal(c.G) + 0.0722 * Canal(c.B);
        }

        private static (double H, double S, double L) AHsl(int r, int g, int b)
        {
            double rf = r / 255.0, gf = g / 255.0, bf = b / 255.0;
            double max = Math.Max(rf, Math.Max(gf, bf)), min = Math.Min(rf, Math.Min(gf, bf));
            double l = (max + min) / 2, d = max - min;
            if (d == 0) return (0, 0, l);
            double s = d / (1 - Math.Abs(2 * l - 1));
            double h = max == rf ? 60 * (((gf - bf) / d) % 6)
                     : max == gf ? 60 * ((bf - rf) / d + 2)
                     : 60 * ((rf - gf) / d + 4);
            return ((h + 360) % 360, s, l);
        }

        private static (int R, int G, int B) ARgb(double h, double s, double l)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
            double m = l - c / 2;
            var (r, g, b) = h switch
            {
                < 60 => (c, x, 0.0),
                < 120 => (x, c, 0.0),
                < 180 => (0.0, c, x),
                < 240 => (0.0, x, c),
                < 300 => (x, 0.0, c),
                _ => (c, 0.0, x),
            };
            static int A255(double v) => (int)Math.Round(Math.Clamp(v, 0, 1) * 255, MidpointRounding.AwayFromZero);
            return (A255(r + m), A255(g + m), A255(b + m));
        }
    }
}
