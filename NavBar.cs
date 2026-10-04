using System.Drawing.Drawing2D;
using FontAwesome.Sharp;

namespace SuraChihIntegrativeProject
{
    // Barra superior compartida por Home y Local
    public class NavBar : Panel
    {
        private readonly Label lblLogo = new();
        private readonly Label lnkCategorias = new();
        private readonly Label lnkMapa = new();
        private readonly RoundedPanel buscador = new();
        private readonly CircleButton btnBuscar = new(IconChar.MagnifyingGlass, 14);
        private readonly TextBox txtBuscar = new();
        private readonly CircleButton btnPerfil = new(IconChar.User, 20);

        public event EventHandler? LogoClick;
        public event EventHandler? MapaClick;

        public NavBar()
        {
            Dock = DockStyle.Top;
            Height = 76;
            BackColor = Theme.Blanco;
            DoubleBuffered = true;

            lblLogo.Text = "SuraChih";
            lblLogo.Font = Theme.Titulo(15, FontStyle.Bold);
            lblLogo.ForeColor = Theme.Negro;
            lblLogo.AutoSize = true;
            lblLogo.Location = new Point(32, 24);
            lblLogo.Cursor = Cursors.Hand;
            lblLogo.Click += (_, e) => LogoClick?.Invoke(this, e);

            ConfigurarEnlace(lnkCategorias, "Categorías");
            ConfigurarEnlace(lnkMapa, "Mapa");
            lnkMapa.Click += (_, e) => MapaClick?.Invoke(this, e);

            buscador.Height = 44;
            buscador.Radio = 22;
            buscador.BackColor = Theme.Blanco;

            btnBuscar.Size = new Size(30, 30);
            btnBuscar.Location = new Point(8, 7);

            txtBuscar.BorderStyle = BorderStyle.None;
            txtBuscar.BackColor = Theme.Blanco;
            txtBuscar.ForeColor = Theme.Negro;
            txtBuscar.Font = Theme.Cuerpo(10.5f);
            txtBuscar.PlaceholderText = "¿Qué te gustaría hacer?";
            txtBuscar.Location = new Point(52, 12);

            buscador.Controls.Add(btnBuscar);
            buscador.Controls.Add(txtBuscar);

            btnPerfil.Size = new Size(48, 48);

            Controls.Add(lblLogo);
            Controls.Add(lnkCategorias);
            Controls.Add(lnkMapa);
            Controls.Add(buscador);
            Controls.Add(btnPerfil);
        }

        private static void ConfigurarEnlace(Label l, string texto)
        {
            l.Text = texto;
            l.Font = Theme.Cuerpo(10.5f);
            l.ForeColor = Theme.Negro;
            l.AutoSize = true;
            l.Cursor = Cursors.Hand;
            l.MouseEnter += (_, _) => l.ForeColor = Theme.Verde;
            l.MouseLeave += (_, _) => l.ForeColor = Theme.Negro;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            lnkCategorias.Location = new Point(lblLogo.Right + 32, (Height - lnkCategorias.Height) / 2);
            lnkMapa.Location = new Point(lnkCategorias.Right + 28, lnkCategorias.Top);

            btnPerfil.Location = new Point(Width - 32 - btnPerfil.Width, (Height - btnPerfil.Height) / 2);
            int izq = lnkMapa.Right + 36;
            buscador.Location = new Point(izq, (Height - buscador.Height) / 2);
            buscador.Width = Math.Max(160, btnPerfil.Left - 28 - izq);
            txtBuscar.Width = Math.Max(40, buscador.Width - txtBuscar.Left - 20);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Sombra suave en el borde inferior
            for (int i = 0; i < 4; i++)
            {
                using var pen = new Pen(Color.FromArgb(18 - i * 4, 0, 0, 0));
                e.Graphics.DrawLine(pen, 0, Height - 1 - i, Width, Height - 1 - i);
            }
        }
    }
}
