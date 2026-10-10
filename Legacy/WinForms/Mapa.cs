using System.Drawing.Drawing2D;
using System.Text.Json;
using FontAwesome.Sharp;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace SuraChihIntegrativeProject
{
    // Encabezado de grupo de fechas ("19 Dic - 21 Dic")
    public class EncabezadoFechas : Control
    {
        private readonly string _texto;

        public EncabezadoFechas(string texto)
        {
            _texto = texto;
            Height = 46;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.Blanco);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using var fuente = Theme.Cuerpo(10);
            using var negro = new SolidBrush(Theme.Negro);
            g.DrawString(_texto, fuente, negro, 14, 13);
            using var borde = new Pen(Theme.Gris);
            g.DrawLine(borde, 0, Height - 1, Width, Height - 1);
        }
    }

    // Fila de la lista lateral: miniatura, título, hora, etiqueta de tipo, precio, calificación y estado
    public class ItemMapa : Control
    {
        private static readonly Color AzulSitio = Color.FromArgb(0x7F, 0xA8, 0xF5);
        private static readonly Color AzulEvento = Color.FromArgb(0x0E, 0x8C, 0xB5);

        private readonly Evento _evento;
        private readonly Image? _imagen;
        private readonly Bitmap _estrella = Theme.Icono(IconChar.Star, 10, Theme.TextoSecundario, IconFont.Regular);
        private bool _encima;
        private bool _seleccionado;

        public Evento Evento => _evento;
        public event EventHandler? Elegido;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool Seleccionado
        {
            get => _seleccionado;
            set { _seleccionado = value; Invalidate(); }
        }

        public ItemMapa(Evento evento)
        {
            _evento = evento;
            Cursor = Cursors.Hand;
            Height = 76 + (evento.Autor != null ? 16 : 0) + (evento.Estado != null ? 24 : 0);
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

            if (evento.Imagen != null)
            {
                string ruta = Path.Combine(AppContext.BaseDirectory, "Assets", evento.Imagen);
                if (File.Exists(ruta)) _imagen = Image.FromFile(ruta);
            }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _encima = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _encima = false; Invalidate(); }
        protected override void OnClick(EventArgs e) { base.OnClick(e); Elegido?.Invoke(this, EventArgs.Empty); }

        // Dibuja una etiqueta redondeada y devuelve su ancho
        private static float Etiqueta(Graphics g, string texto, Font fuente, float x, float y, Color fondo, Color letra)
        {
            var medida = g.MeasureString(texto, fuente);
            var r = new RectangleF(x, y, medida.Width + 6, medida.Height + 1);
            using var camino = Dibujo.Redondeado(r, 4);
            using var pincel = new SolidBrush(fondo);
            using var tinta = new SolidBrush(letra);
            g.FillPath(pincel, camino);
            g.DrawString(texto, fuente, tinta, x + 3, y + 0.5f);
            return r.Width;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(_seleccionado ? Color.FromArgb(0xE8, 0xF0, 0xEC) : _encima ? Color.FromArgb(0xF0, 0xF1, 0xF3) : Theme.Blanco);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Miniatura
            var foto = new RectangleF(14, 10, 56, 56);
            using (var camino = Dibujo.Redondeado(foto, 6))
            {
                var estado = g.Save();
                g.SetClip(camino);
                if (_imagen != null) Dibujo.ImagenCubriendo(g, _imagen, foto);
                else
                {
                    using var degradado = new LinearGradientBrush(foto, _evento.ColorRelleno, ControlPaint.Dark(_evento.ColorRelleno, 0.25f), 60f);
                    g.FillRectangle(degradado, foto);
                }
                g.Restore(estado);
            }

            using var fTitulo = Theme.Cuerpo(9.5f, FontStyle.Bold);
            using var fPequena = Theme.Cuerpo(8f);
            using var fEtiqueta = Theme.Cuerpo(7.5f);
            using var negro = new SolidBrush(Theme.Negro);
            using var secundario = new SolidBrush(Theme.TextoSecundario);

            const float x = 82;
            g.DrawString(_evento.Titulo, fTitulo, negro, x, 8);

            // Hora + tipo
            g.DrawString(_evento.Hora, fPequena, secundario, x, 29);
            float xTipo = x + g.MeasureString(_evento.Hora, fPequena).Width + 4;
            if (_evento.Tipo.Length > 0)
                Etiqueta(g, _evento.Tipo, fEtiqueta, xTipo, 29, _evento.Tipo == "Event" ? AzulEvento : AzulSitio, Theme.Blanco);

            // Precio y calificación
            string precio = $"$ - {_evento.Precio}";
            g.DrawString(precio, fPequena, negro, x, 48);
            if (_evento.Calificacion > 0)
            {
                float xs = x + g.MeasureString(precio, fPequena).Width + 4;
                g.DrawImage(_estrella, xs, 50);
                g.DrawString($"- {_evento.Calificacion:0.0#}", fPequena, secundario, xs + _estrella.Width, 48);
            }

            float y = 66;
            if (_evento.Autor != null)
            {
                g.DrawString($"By {_evento.Autor}", fPequena, secundario, x, y);
                y += 16;
            }
            if (_evento.Estado != null)
            {
                bool agotado = _evento.Estado == "Sold Out";
                Etiqueta(g, _evento.Estado, fEtiqueta, x, y + 2,
                    agotado ? Color.FromArgb(0xFE, 0xB4, 0xB4) : Color.FromArgb(0xFD, 0xE6, 0x8A),
                    agotado ? Color.FromArgb(0xB9, 0x1C, 0x1C) : Color.FromArgb(0x92, 0x40, 0x0E));
            }

            using var borde = new Pen(Theme.Gris);
            g.DrawLine(borde, 14, Height - 2, Width - 14, Height - 2);
        }
    }

    // Pantalla de mapa: lista de eventos a la izquierda y mapa (WebView2 + Leaflet) a la derecha
    public class Mapa : Form
    {
        private const string Host = "app.sura.local";

        private readonly NavBar navbar = new();
        private readonly FlowLayoutPanel lista = new();
        private readonly WebView2 web = new();
        private readonly List<ItemMapa> items = new();
        private readonly List<Evento> eventos = Datos();

        public Mapa()
        {
            Text = "SuraChih - Mapa";
            BackColor = Theme.Blanco;
            ClientSize = new Size(1100, 700);
            MinimumSize = new Size(760, 560);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            lista.Dock = DockStyle.Left;
            lista.Width = 290;
            lista.FlowDirection = FlowDirection.TopDown;
            lista.WrapContents = false;
            lista.AutoScroll = true;
            lista.BackColor = Theme.Blanco;
            lista.Resize += (_, _) => AjustarAncho();

            web.Dock = DockStyle.Fill;
            web.DefaultBackgroundColor = Theme.Blanco;

            ConstruirLista();

            Controls.Add(web);
            Controls.Add(lista);
            Controls.Add(navbar);

            navbar.LogoClick += (_, _) => Close();
            Load += async (_, _) => await IniciarMapa();
        }

        // Datos de ejemplo hasta conectarlos con la base de datos
        private static List<Evento> Datos() => new()
        {
            new Evento { Titulo = "La libertad", Fechas = "19 Dic - 21 Dic", Hora = "9:30AM", Tipo = "Sitio", Precio = "200", Calificacion = 4.80, Imagen = "evento1.jpg", ColorRelleno = Color.FromArgb(0x8A, 0x86, 0x7E), Latitud = 28.6353, Longitud = -106.0889 },
            new Evento { Titulo = "Candlelight Show", Fechas = "19 Dic - 21 Dic", Hora = "9:30AM", Tipo = "Event", Precio = "Free", Autor = "Fever", Estado = "Sold Out", Imagen = "evento5.jpg", ColorRelleno = Color.FromArgb(0x7E, 0x3F, 0xA8), Latitud = 28.6540, Longitud = -106.0720 },
            new Evento { Titulo = "La Gracia", Fechas = "19 Dic - 21 Dic", Hora = "10:30AM", Tipo = "Sitio", Precio = "150", Calificacion = 5.0, Imagen = "evento2.jpg", ColorRelleno = Color.FromArgb(0x7B, 0x5A, 0x3E), Latitud = 28.6210, Longitud = -106.0990 },
            new Evento { Titulo = "Farid Conferencia", Fechas = "Dec 21 - Dec 24", Hora = "9:30AM", Tipo = "Event", Precio = "Free", Autor = "Farid", Estado = "Near Capacity", Imagen = "evento4.jpg", ColorRelleno = Color.FromArgb(0x30, 0x30, 0x34), Latitud = 28.6700, Longitud = -106.1100 },
        };

        private void ConstruirLista()
        {
            string? grupoActual = null;
            foreach (var ev in eventos)
            {
                if (ev.Fechas != grupoActual)
                {
                    grupoActual = ev.Fechas;
                    lista.Controls.Add(new EncabezadoFechas(grupoActual) { Margin = Padding.Empty });
                }

                var item = new ItemMapa(ev) { Margin = Padding.Empty };
                item.Elegido += (_, _) => Seleccionar(item, true);
                items.Add(item);
                lista.Controls.Add(item);
            }
            AjustarAncho();
        }

        // Cada fila ocupa todo el ancho útil (sin la barra de scroll)
        private void AjustarAncho()
        {
            int ancho = lista.ClientSize.Width;
            foreach (Control c in lista.Controls) c.Width = ancho;
        }

        private async void Seleccionar(ItemMapa item, bool moverMapa)
        {
            foreach (var i in items) i.Seleccionado = i == item;
            if (moverMapa && web.CoreWebView2 != null)
                await web.CoreWebView2.ExecuteScriptAsync($"seleccionar({items.IndexOf(item)})");
        }

        private async Task IniciarMapa()
        {
            try
            {
                // La carpeta de datos del navegador va en AppData, no junto al ejecutable
                string datos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SuraChih", "WebView2");
                var entorno = await CoreWebView2Environment.CreateAsync(null, datos);
                await web.EnsureCoreWebView2Async(entorno);

                // Se sirve mapa.html desde un host virtual para que los mosaicos del mapa reciban un Referer válido
                web.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    Host, Path.Combine(AppContext.BaseDirectory, "Assets"), CoreWebView2HostResourceAccessKind.Allow);
                web.CoreWebView2.WebMessageReceived += AlRecibirMensaje;
                web.CoreWebView2.Navigate($"https://{Host}/mapa.html");
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo iniciar el mapa (¿está instalado WebView2 Runtime?):\n" + ex.Message,
                    "Mapa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void AlRecibirMensaje(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string mensaje = e.TryGetWebMessageAsString();
            if (mensaje == "listo")
            {
                var puntos = eventos.Select((ev, i) => new { id = i, titulo = ev.Titulo, lat = ev.Latitud, lng = ev.Longitud });
                await web.CoreWebView2.ExecuteScriptAsync($"setEventos({JsonSerializer.Serialize(puntos)})");
            }
            else if (int.TryParse(mensaje, out int id) && id >= 0 && id < items.Count)
            {
                Seleccionar(items[id], false);
                lista.ScrollControlIntoView(items[id]);
                await web.CoreWebView2.ExecuteScriptAsync($"seleccionar({id})");
            }
        }
    }
}
