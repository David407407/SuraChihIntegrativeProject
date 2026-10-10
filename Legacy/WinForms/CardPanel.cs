using System.Drawing.Drawing2D;

namespace SuraChihIntegrativeProject
{
    public class CardPanel : Panel
    {
        private const int Margen = 20;   // espacio reservado para la sombra
        private const int Radio = 16;

        public CardPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.FromArgb(0xF9, 0xFA, 0xFB);
        }

        private static GraphicsPath Redondeado(Rectangle r, int radio)
        {
            int d = radio * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var card = new Rectangle(Margen + 4, Margen, Width - 2 * (Margen + 4), Height - 2 * Margen);

            // Sombra: capas concéntricas con poca opacidad, desplazadas hacia abajo
            for (int i = Margen; i >= 1; i--)
            {
                var r = Rectangle.Inflate(card, i, i);
                r.Y += 4;
                using var path = Redondeado(r, Radio + i);
                using var brush = new SolidBrush(Color.FromArgb(3, 0, 0, 0));
                g.FillPath(brush, path);
            }

            // Card
            using var cardPath = Redondeado(card, Radio);
            using var fill = new SolidBrush(Color.FromArgb(0xF9, 0xFA, 0xFB));
            using var borde = new Pen(Color.FromArgb(230, 232, 235));
            g.FillPath(fill, cardPath);
            g.DrawPath(borde, cardPath);
        }
    }
}