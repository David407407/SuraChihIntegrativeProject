// namespace SuraChihIntegrativeProject
// {
//     public partial class Form1 : Form
//     {
//         public Form1()
//         {
//             InitializeComponent();
//         }

//         private void checkBox1_CheckedChanged(object sender, EventArgs e)
//         {

//         }
//     }
// }

using BackendLogica;

namespace SuraChihIntegrativeProject
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }

        private void Login_Load(object sender, EventArgs e)
        {
            // Anchor = None mantiene la card centrada al redimensionar
            card.Location = new Point((ClientSize.Width - card.Width) / 2,
                                      (ClientSize.Height - card.Height) / 2);
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            lblError.Text = "";
            string usuario = txtUsuario.Text.Trim();
            string password = txtPassword.Text;

            if (usuario.Length == 0 || password.Length == 0)
            {
                lblError.Text = "Completa ambos campos.";
                return;
            }

            btnLogin.Enabled = false;
            btnLogin.Text = "Verificando...";

            try
            {
                // Fuera del hilo de UI para no congelar la ventana
                var tabla = await Task.Run(() => new ConexionDB().ValidarLogin(usuario, password));

                if (tabla.Rows.Count == 0)
                {
                    lblError.Text = "Credenciales incorrectas.";
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                var fila = tabla.Rows[0];
                var home = new Home();
                home.FormClosed += (_, _) => Close();
                home.Show();
                Hide();
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "Login";
            }
        }
    }
}