using FontAwesome.Sharp;

namespace SuraChihIntegrativeProject
{
    // Pantalla principal. Se construye en código (sin diseñador) para poder dibujar los controles a medida.
    public class Home : Form
    {
        private readonly NavBar navbar = new();
        private readonly HeroPanel hero = new();

        private readonly Panel contenido = new();
        private readonly Label lblTop = new();
        private readonly CircleButton btnVerMas = new(IconChar.ArrowRight, 14);
        private readonly FlowLayoutPanel tarjetas = new();

        public Home()
        {
            Text = "SuraChih";
            BackColor = Theme.Blanco;
            ClientSize = new Size(1100, 700);
            MinimumSize = new Size(760, 560);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            ConstruirHero();
            ConstruirContenido();

            // El orden de Add importa con Dock: lo último se acomoda primero
            Controls.Add(contenido);
            Controls.Add(hero);
            Controls.Add(navbar);

            navbar.MapaClick += (_, _) => Navegacion.Abrir(this, new Mapa());

            Resize += (_, _) => Acomodar();
            Load += (_, _) => Acomodar();
        }

        private void ConstruirHero()
        {
            hero.Dock = DockStyle.Top;
            hero.Height = 240;
            hero.Titulo = "Chihuahua";
            hero.Subtitulo = "Cosas que hacer: eventos, experiencias y mucho más";
            hero.CargarImagen(Path.Combine(AppContext.BaseDirectory, "Assets", "hero.jpg"));
        }

        private void ConstruirContenido()
        {
            contenido.Dock = DockStyle.Fill;
            contenido.BackColor = Theme.Blanco;

            lblTop.Text = "Top 5 cosas que hacer en Chihuahua";
            lblTop.Font = Theme.Titulo(14, FontStyle.Bold);
            lblTop.ForeColor = Theme.Negro;
            lblTop.AutoSize = true;
            lblTop.Location = new Point(32, 28);

            btnVerMas.Size = new Size(36, 36);

            tarjetas.Location = new Point(32, 80);
            tarjetas.WrapContents = false;
            tarjetas.AutoScroll = true;
            tarjetas.BackColor = Theme.Blanco;
            tarjetas.Padding = new Padding(0, 0, 0, 4);

            // Datos de ejemplo hasta conectarlos con la base de datos.
            // Las fotos van en Assets (por ejemplo Assets\evento1.jpg); sin foto se muestra un color.
            var eventos = new[]
            {
                new Evento { Titulo = "Home in Chihuahua", Fechas = "19 Dic - 21 Dic", Precio = "Free", Calificacion = 4.80, Imagen = "evento1.jpg", ColorRelleno = Color.FromArgb(0x8A, 0x86, 0x7E) },
                new Evento { Titulo = "La Gracia", Fechas = "19 Dic - 21 Dic", Precio = "Free", Calificacion = 4.80, Imagen = "evento2.jpg", ColorRelleno = Color.FromArgb(0x7B, 0x5A, 0x3E) },
                new Evento { Titulo = "the (usual)*", Fechas = "19 Dic - 21 Dic", Precio = "Free", Calificacion = 4.80, Imagen = "evento3.jpg", ColorRelleno = Color.FromArgb(0x6E, 0x8B, 0x74) },
                new Evento { Titulo = "Farid Dieck Conferencia", Fechas = "19 Dic - 21 Dic", Precio = "Free", Calificacion = 4.80, Imagen = "evento4.jpg", ColorRelleno = Color.FromArgb(0x30, 0x30, 0x34) },
                new Evento { Titulo = "Candlelight", Fechas = "19 Dic - 21 Dic", Precio = "Free", Calificacion = 4.80, Imagen = "evento5.jpg", ColorRelleno = Color.FromArgb(0x7E, 0x3F, 0xA8) },
            };

            foreach (var ev in eventos)
            {
                var card = new EventCard(ev) { Margin = new Padding(0, 0, 24, 0) };
                card.Abrir += (_, _) => AbrirLocal(ev);
                tarjetas.Controls.Add(card);
            }

            contenido.Controls.Add(lblTop);
            contenido.Controls.Add(btnVerMas);
            contenido.Controls.Add(tarjetas);
        }

        // Abre el detalle del local y vuelve a mostrar Home cuando se cierra
        private void AbrirLocal(Evento ev) => Navegacion.Abrir(this, new Local(ev));

        private void Acomodar()
        {
            btnVerMas.Location = new Point(lblTop.Right + 14, lblTop.Top + (lblTop.Height - btnVerMas.Height) / 2);
            tarjetas.Size = new Size(Math.Max(0, contenido.Width - 32), Math.Max(0, contenido.Height - tarjetas.Top - 16));
        }
    }
}
