using System.Drawing.Drawing2D;
using FontAwesome.Sharp;

namespace SuraChihIntegrativeProject
{
    // Foto con esquinas redondeadas; si no existe el archivo se dibuja un degradado de relleno
    public class FotoRedondeada : Control
    {
        private Image? _imagen;
        private readonly Color _relleno;
        private readonly Bitmap? _icono;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Radio { get; set; } = 12;

        public FotoRedondeada(string? archivo, Color relleno, IconChar? icono = null)
        {
            _relleno = relleno;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            if (archivo != null)
            {
                string ruta = Path.Combine(AppContext.BaseDirectory, "Assets", archivo);
                if (File.Exists(ruta)) _imagen = Image.FromFile(ruta);
            }
            if (icono != null) _icono = Theme.Icono(icono.Value, 28, Theme.Blanco);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Blanco);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            var r = new RectangleF(0, 0, Width - 1, Height - 1);
            using var camino = Dibujo.Redondeado(r, Radio);
            g.SetClip(camino);
            if (_imagen != null) Dibujo.ImagenCubriendo(g, _imagen, r);
            else
            {
                using var degradado = new LinearGradientBrush(r, _relleno, ControlPaint.Dark(_relleno, 0.2f), 60f);
                g.FillRectangle(degradado, r);
                if (_icono != null) g.DrawImage(_icono, (Width - _icono.Width) / 2, (Height - _icono.Height) / 2);
            }
        }
    }

    // Collage de 5 fotos: grande, alta, dos apiladas y grande. Solo las esquinas exteriores van redondeadas.
    public class GaleriaPanel : Control
    {
        private const int Espacio = 14;
        private readonly Image?[] _fotos = new Image?[5];
        private readonly Color[] _colores;

        public GaleriaPanel(string[] archivos, Color[] colores)
        {
            _colores = colores;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            for (int i = 0; i < _fotos.Length && i < archivos.Length; i++)
            {
                string ruta = Path.Combine(AppContext.BaseDirectory, "Assets", archivos[i]);
                if (File.Exists(ruta)) _fotos[i] = Image.FromFile(ruta);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Blanco);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            using var marco = Dibujo.Redondeado(new RectangleF(0, 0, Width - 1, Height - 1), 14);
            g.SetClip(marco);

            float util = Width - 1 - 3 * Espacio;
            float[] anchos = { util * 0.370f, util * 0.213f, util * 0.139f, util * 0.279f };
            float alto = Height - 1;
            float mitad = (alto - Espacio) / 2;

            var celdas = new RectangleF[5];
            float x = 0;
            celdas[0] = new RectangleF(x, 0, anchos[0], alto); x += anchos[0] + Espacio;
            celdas[1] = new RectangleF(x, 0, anchos[1], alto); x += anchos[1] + Espacio;
            celdas[2] = new RectangleF(x, 0, anchos[2], mitad);
            celdas[3] = new RectangleF(x, mitad + Espacio, anchos[2], mitad); x += anchos[2] + Espacio;
            celdas[4] = new RectangleF(x, 0, anchos[3], alto);

            for (int i = 0; i < celdas.Length; i++)
            {
                // Cada foto se recorta a su celda y al marco redondeado
                g.SetClip(marco);
                g.SetClip(celdas[i], CombineMode.Intersect);
                if (_fotos[i] != null) Dibujo.ImagenCubriendo(g, _fotos[i]!, celdas[i]);
                else
                {
                    using var degradado = new LinearGradientBrush(celdas[i], _colores[i % _colores.Length],
                        ControlPaint.Dark(_colores[i % _colores.Length], 0.25f), 60f);
                    g.FillRectangle(degradado, celdas[i]);
                }
            }
        }
    }

    // Mini calendario "Dec / 21"
    public class CalendarTile : Control
    {
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Mes { get; set; } = "Dec";
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Dia { get; set; } = "21";

        public CalendarTile()
        {
            Size = new Size(34, 42);
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Blanco);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            using var camino = Dibujo.Redondeado(r, 5);
            using var relleno = new SolidBrush(Theme.Blanco);
            using var borde = new Pen(Theme.Gris);
            g.FillPath(relleno, camino);

            var estado = g.Save();
            g.SetClip(camino);
            using (var cabecera = new SolidBrush(Theme.Gris))
                g.FillRectangle(cabecera, 0, 0, Width, 13);
            g.Restore(estado);
            g.DrawPath(borde, camino);

            using var fMes = Theme.Cuerpo(6.5f, FontStyle.Bold);
            using var fDia = Theme.Titulo(11, FontStyle.Bold);
            using var negro = new SolidBrush(Theme.Negro);
            var centro = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(Mes, fMes, negro, new RectangleF(0, 0, Width, 13), centro);
            g.DrawString(Dia, fDia, negro, new RectangleF(0, 13, Width, Height - 13), centro);
        }
    }

    // Detalle de un local: galería, descripción y tarjeta de reserva
    public class Local : Form
    {
        private const string Lorem =
            "Proin sed magna sed elit tincidunt tincidunt sodales eu massa. Cras accumsan gravida malesuada. Nullam hendrerit lorem ex, " +
            "eu porta dolor tristique ac. Cras id quam dui. Aenean a nibh turpis. Praesent tincidunt dui vel eros varius, ac mollis turpis " +
            "lacinia. Donec nec porta libero. Sed nulla nulla, tristique quis turpis nec, egestas feugiat mauris. Sed et justo vitae nibh " +
            "tincidunt laoreet sit amet eget dolor. Integer laoreet id erat eu aliquet.";

        private readonly NavBar navbar = new();
        private readonly Panel scroll = new();
        private readonly Panel pagina = new();

        private readonly GaleriaPanel galeria;
        private readonly Label lblTitulo = new();
        private readonly PictureBox picEstrella = new();
        private readonly Label lblCalificacion = new();
        private readonly Label lblDescripcion = new();
        private readonly Label lblPuntos = new();
        private readonly CircleButton btnCompartir = new(IconChar.ArrowUpFromBracket, 15);
        private readonly CircleButton btnFavorito = new(IconChar.Heart, 15);
        private readonly RoundedPanel reserva = new();
        private readonly Label lblPrecio = new();
        private readonly Panel separador = new();
        private readonly Label lblFecha = new();
        private readonly CalendarTile calendario = new();
        private readonly FotoRedondeada mapa;

        public Local(Evento evento)
        {
            Text = evento.Titulo;
            BackColor = Theme.Blanco;
            ClientSize = new Size(1100, 700);
            MinimumSize = new Size(760, 560);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            galeria = new GaleriaPanel(
                new[] { "local1.jpg", "local2.jpg", "local3.jpg", "local4.jpg", "local5.jpg" },
                new[]
                {
                    Color.FromArgb(0xB8, 0xA8, 0x8A), Color.FromArgb(0x8B, 0x6B, 0x4A), Color.FromArgb(0xC9, 0xB9, 0x9D),
                    Color.FromArgb(0x9A, 0x8F, 0x86), Color.FromArgb(0x5E, 0x5A, 0x55),
                });
            mapa = new FotoRedondeada("mapa.png", Color.FromArgb(0xA9, 0xD3, 0xB5), IconChar.LocationDot) { Radio = 8 };

            ConstruirTextos(evento);
            ConstruirReserva();

            pagina.Dock = DockStyle.Top;
            pagina.BackColor = Theme.Blanco;
            pagina.Controls.AddRange(new Control[]
            {
                galeria, lblTitulo, picEstrella, lblCalificacion, lblDescripcion, lblPuntos, btnCompartir, btnFavorito, reserva,
            });
            pagina.Resize += (_, _) => AcomodarPagina();

            scroll.Dock = DockStyle.Fill;
            scroll.AutoScroll = true;
            scroll.BackColor = Theme.Blanco;
            scroll.Controls.Add(pagina);
            scroll.Resize += (_, _) => CentrarPagina();

            Controls.Add(scroll);
            Controls.Add(navbar);

            navbar.LogoClick += (_, _) => Close();
            navbar.MapaClick += (_, _) => Navegacion.Abrir(this, new Mapa());
            Load += (_, _) => CentrarPagina();
        }

        private void ConstruirTextos(Evento evento)
        {
            lblTitulo.Text = evento.Titulo;
            lblTitulo.Font = Theme.Titulo(22, FontStyle.Bold);
            lblTitulo.ForeColor = Theme.Negro;
            lblTitulo.AutoSize = true;

            picEstrella.Image = Theme.Icono(IconChar.Star, 16, Theme.TextoSecundario, IconFont.Regular);
            picEstrella.SizeMode = PictureBoxSizeMode.CenterImage;
            picEstrella.Size = new Size(22, 22);

            lblCalificacion.Text = $"- {evento.Calificacion:0.00}";
            lblCalificacion.Font = Theme.Cuerpo(10.5f, FontStyle.Bold);
            lblCalificacion.ForeColor = Theme.Negro;
            lblCalificacion.AutoSize = true;

            lblDescripcion.Text = Lorem;
            lblDescripcion.Font = Theme.Cuerpo(9);
            lblDescripcion.ForeColor = Theme.Negro;
            lblDescripcion.AutoSize = true;

            const string punto = "Cras id quam dui. Aenean a nibh turpis.";
            lblPuntos.Text = string.Join(Environment.NewLine, Enumerable.Repeat("•   " + punto, 4));
            lblPuntos.Font = Theme.Cuerpo(9);
            lblPuntos.ForeColor = Theme.Negro;
            lblPuntos.AutoSize = true;

            btnCompartir.Size = new Size(36, 36);
            btnFavorito.Size = new Size(36, 36);
        }

        private void ConstruirReserva()
        {
            reserva.Radio = 12;
            reserva.BackColor = Theme.Blanco;
            reserva.Relleno = Theme.Blanco;
            reserva.Borde = Theme.Gris;

            lblPrecio.Text = "$ - 200 en promedio";
            lblPrecio.Font = Theme.Titulo(11, FontStyle.Bold);
            lblPrecio.ForeColor = Theme.Negro;
            lblPrecio.AutoSize = true;
            lblPrecio.Location = new Point(18, 16);

            separador.BackColor = Theme.Negro;
            separador.Height = 1;

            lblFecha.Text = "Selecciona una fecha";
            lblFecha.Font = Theme.Cuerpo(9, FontStyle.Bold);
            lblFecha.ForeColor = Theme.Negro;
            lblFecha.AutoSize = true;

            reserva.Controls.AddRange(new Control[] { lblPrecio, separador, lblFecha, calendario, mapa });
            reserva.Resize += (_, _) => AcomodarReserva();
        }

        // Márgenes laterales para centrar la página con ancho máximo
        private void CentrarPagina()
        {
            int ancho = Math.Min(scroll.ClientSize.Width - 64, 960);
            int lateral = Math.Max(32, (scroll.ClientSize.Width - ancho) / 2);
            scroll.Padding = new Padding(lateral, 24, lateral, 24);
            AcomodarPagina();
        }

        private void AcomodarPagina()
        {
            int w = pagina.Width;
            if (w <= 0) return;

            galeria.SetBounds(0, 0, w, (int)(w * 0.36));

            int y = galeria.Bottom + 24;
            int anchoReserva = Math.Max(220, (int)(w * 0.30));
            int anchoTexto = w - anchoReserva - 36;

            lblTitulo.Location = new Point(0, y);
            picEstrella.Location = new Point(0, lblTitulo.Bottom + 2);
            lblCalificacion.Location = new Point(picEstrella.Right, picEstrella.Top + 3);

            btnFavorito.Location = new Point(anchoTexto - btnFavorito.Width, y + 4);
            btnCompartir.Location = new Point(btnFavorito.Left - 10 - btnCompartir.Width, y + 4);

            lblDescripcion.MaximumSize = new Size(anchoTexto, 0);
            lblDescripcion.Location = new Point(0, picEstrella.Bottom + 22);
            lblPuntos.MaximumSize = new Size(anchoTexto, 0);
            lblPuntos.Location = new Point(10, lblDescripcion.Bottom + 14);

            reserva.SetBounds(w - anchoReserva, y, anchoReserva, 320);

            pagina.Height = Math.Max(lblPuntos.Bottom, reserva.Bottom) + 24;
        }

        private void AcomodarReserva()
        {
            int w = reserva.Width;
            separador.SetBounds(18, 52, w - 36, 1);
            lblFecha.Location = new Point(18, 72);
            calendario.Location = new Point(w - 18 - calendario.Width, 66);
            mapa.SetBounds(18, 124, w - 36, reserva.Height - 124 - 18);
        }
    }
}
