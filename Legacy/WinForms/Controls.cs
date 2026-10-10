using System.Drawing.Drawing2D;
using FontAwesome.Sharp;

namespace SuraChihIntegrativeProject
{
    internal static class Dibujo
    {
        public static GraphicsPath Redondeado(RectangleF r, float radio)
        {
            float d = radio * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        // Dibuja la imagen llenando el rectángulo (recorta lo que sobra, sin deformar)
        public static void ImagenCubriendo(Graphics g, Image img, RectangleF destino)
        {
            float escala = Math.Max(destino.Width / img.Width, destino.Height / img.Height);
            float w = img.Width * escala, h = img.Height * escala;
            g.DrawImage(img, destino.X + (destino.Width - w) / 2, destino.Y + (destino.Height - h) / 2, w, h);
        }
    }

    // Botón circular verde con un icono blanco (lupa, usuario, flecha)
    public class CircleButton : Control
    {
        private readonly Bitmap _icono;
        private readonly int _tamañoIcono;

        public CircleButton(IconChar icono, int tamañoIcono)
        {
            _tamañoIcono = tamañoIcono;
            _icono = Theme.Icono(icono, tamañoIcono, Theme.Blanco);
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pincel = new SolidBrush(Theme.Verde);
            g.FillEllipse(pincel, 0, 0, Width - 1, Height - 1);
            g.DrawImage(_icono, (Width - _icono.Width) / 2, (Height - _icono.Height) / 2);
        }
    }

    // Panel con esquinas redondeadas y borde, para la barra de búsqueda
    public class RoundedPanel : Panel
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Radio { get; set; } = 20;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color Relleno { get; set; } = Theme.Blanco;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color Borde { get; set; } = Theme.Gris;

        public RoundedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Blanco);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using var camino = Dibujo.Redondeado(r, Radio);
            using var relleno = new SolidBrush(Relleno);
            using var borde = new Pen(Borde);
            g.FillPath(relleno, camino);
            g.DrawPath(borde, camino);
        }
    }

    // Banner con la imagen de la ciudad, un velo oscuro y el título
    public class HeroPanel : Control
    {
        private Image? _fondo;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]

        public string Titulo { get; set; } = "";
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Subtitulo { get; set; } = "";

        public HeroPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        public void CargarImagen(string ruta)
        {
            if (File.Exists(ruta)) _fondo = Image.FromFile(ruta);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            var area = new RectangleF(0, 0, Width, Height);

            if (_fondo != null)
                Dibujo.ImagenCubriendo(g, _fondo, area);
            else
            {
                // Sin imagen: degradado oscuro de relleno
                using var degradado = new LinearGradientBrush(ClientRectangle, Color.FromArgb(0x3A, 0x3F, 0x45), Theme.Negro, 20f);
                g.FillRectangle(degradado, ClientRectangle);
            }

            using (var velo = new SolidBrush(Color.FromArgb(110, Theme.Negro)))
                g.FillRectangle(velo, ClientRectangle);

            int x = Math.Max(32, Width / 14);
            using var fTitulo = Theme.Titulo(36);
            using var fSub = Theme.Titulo(13, FontStyle.Regular);
            using var blanco = new SolidBrush(Theme.Blanco);
            var altoTitulo = g.MeasureString(Titulo, fTitulo).Height;
            var altoSub = g.MeasureString(Subtitulo, fSub).Height;
            float y = (Height - altoTitulo - altoSub) / 2 + 6;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.DrawString(Titulo, fTitulo, blanco, x, y);
            g.DrawString(Subtitulo, fSub, blanco, x + 2, y + altoTitulo - 4);
        }
    }

    public class Evento
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Titulo { get; set; } = "";
        public string Fechas { get; set; } = "";
        public string Precio { get; set; } = "";
        public double Calificacion { get; set; }
        public string? Imagen { get; set; }          // ruta relativa dentro de Assets
        public Color ColorRelleno { get; set; } = Color.Gray;  // se usa si no hay imagen

        // Datos para la pantalla de mapa
        public string Hora { get; set; } = "";
        public string Tipo { get; set; } = "";       // "Sitio" o "Event"
        public string? Autor { get; set; }
        public string? Estado { get; set; }          // "Sold Out", "Near Capacity"...
        public double Latitud { get; set; }
        public double Longitud { get; set; }
    }

    // Tarjeta de evento: foto redondeada con corazón, título, fechas, precio y calificación
    public class EventCard : Control
    {
        private const int AltoImagen = 150;
        private readonly Evento _evento;
        private readonly Image? _imagen;
        private readonly Bitmap _corazonVacio = Theme.Icono(IconChar.Heart, 20, Theme.Blanco, IconFont.Regular);
        private readonly Bitmap _corazonLleno = Theme.Icono(IconChar.Heart, 20, Theme.Blanco);
        private readonly Bitmap _estrella = Theme.Icono(IconChar.Star, 11, Theme.TextoSecundario, IconFont.Regular);
        private Rectangle _zonaCorazon;

        public bool Favorito { get; private set; }

        // Se dispara al hacer clic en la tarjeta (fuera del corazón)
        public event EventHandler? Abrir;

        public EventCard(Evento evento)
        {
            _evento = evento;
            Size = new Size(150, AltoImagen + 78);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

            if (evento.Imagen != null)
            {
                string ruta = Path.Combine(AppContext.BaseDirectory, "Assets", evento.Imagen);
                if (File.Exists(ruta)) _imagen = Image.FromFile(ruta);
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (_zonaCorazon.Contains(e.Location))
            {
                Favorito = !Favorito;
                Invalidate();
            }
            else
                Abrir?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Blanco);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            var foto = new RectangleF(0, 0, Width - 1, AltoImagen);
            using (var camino = Dibujo.Redondeado(foto, 12))
            {
                var estado = g.Save();
                g.SetClip(camino);
                if (_imagen != null)
                    Dibujo.ImagenCubriendo(g, _imagen, foto);
                else
                {
                    using var degradado = new LinearGradientBrush(foto, _evento.ColorRelleno,
                        ControlPaint.Dark(_evento.ColorRelleno, 0.25f), 60f);
                    g.FillRectangle(degradado, foto);
                }
                g.Restore(estado);
            }

            var corazon = Favorito ? _corazonLleno : _corazonVacio;
            _zonaCorazon = new Rectangle(Width - corazon.Width - 14, 8, corazon.Width + 8, corazon.Height + 8);
            g.DrawImage(corazon, _zonaCorazon.X + 4, _zonaCorazon.Y + 4);

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using var fTitulo = Theme.Cuerpo(9.5f);
            using var fPequena = Theme.Cuerpo(8f);
            using var negro = new SolidBrush(Theme.Negro);
            using var secundario = new SolidBrush(Theme.TextoSecundario);
            var formato = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

            g.DrawString(_evento.Titulo, fTitulo, negro, new RectangleF(0, AltoImagen + 8, Width, 20), formato);
            g.DrawString(_evento.Fechas, fPequena, secundario, 0, AltoImagen + 30);

            // Línea de precio: "$ - Free  ☆ - 4.80"
            float y = AltoImagen + 50;
            string precio = $"$ - {_evento.Precio}";
            g.DrawString(precio, fPequena, secundario, 0, y);
            float x = g.MeasureString(precio, fPequena).Width + 6;
            g.DrawImage(_estrella, x, y + 1);
            g.DrawString($"- {_evento.Calificacion:0.00}", fPequena, secundario, x + _estrella.Width + 2, y);
        }
    }
}

