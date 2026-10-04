// namespace SuraChihIntegrativeProject
// {
//     partial class Form1
//     {
//         /// <summary>
//         ///  Required designer variable.
//         /// </summary>
//         private System.ComponentModel.IContainer components = null;

//         /// <summary>
//         ///  Clean up any resources being used.
//         /// </summary>
//         /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
//         protected override void Dispose(bool disposing)
//         {
//             if (disposing && (components != null))
//             {
//                 components.Dispose();
//             }
//             base.Dispose(disposing);
//         }

//         #region Windows Form Designer generated code

//         /// <summary>
//         ///  Required method for Designer support - do not modify
//         ///  the contents of this method with the code editor.
//         /// </summary>
//         private void InitializeComponent()
//         {
//             checkBox1 = new CheckBox();
//             SuspendLayout();
//             // 
//             // checkBox1
//             // 
//             checkBox1.AutoSize = true;
//             checkBox1.Location = new Point(43, 42);
//             checkBox1.Name = "checkBox1";
//             checkBox1.Size = new Size(51, 19);
//             checkBox1.TabIndex = 0;
//             checkBox1.Text = "Hola";
//             checkBox1.UseVisualStyleBackColor = true;
//             checkBox1.CheckedChanged += checkBox1_CheckedChanged;
//             // 
//             // Form1
//             // 
//             AutoScaleDimensions = new SizeF(7F, 15F);
//             AutoScaleMode = AutoScaleMode.Font;
//             ClientSize = new Size(800, 450);
//             Controls.Add(checkBox1);
//             Name = "Form1";
//             Text = "Form1";
//             ResumeLayout(false);
//             PerformLayout();
//         }

//         #endregion

//         private CheckBox checkBox1;
//     }
// }

namespace SuraChihIntegrativeProject
{
    partial class Login
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            card = new CardPanel();
            lblTitulo = new Label();
            lblSubtitulo = new Label();
            lblUsuario = new Label();
            txtUsuario = new TextBox();
            lblPassword = new Label();
            txtPassword = new TextBox();
            lblError = new Label();
            btnLogin = new Button();
            card.SuspendLayout();
            SuspendLayout();
            //
            // card
            //
            card.Anchor = AnchorStyles.None;
            card.Size = new Size(440, 400);
            card.Controls.Add(lblTitulo);
            card.Controls.Add(lblSubtitulo);
            card.Controls.Add(lblUsuario);
            card.Controls.Add(txtUsuario);
            card.Controls.Add(lblPassword);
            card.Controls.Add(txtPassword);
            card.Controls.Add(lblError);
            card.Controls.Add(btnLogin);
            //
            // lblTitulo
            //
            lblTitulo.AutoSize = true;
            lblTitulo.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblTitulo.ForeColor = Color.FromArgb(0x11, 0x18, 0x27);
            lblTitulo.BackColor = Color.Transparent;
            lblTitulo.Location = new Point(56, 52);
            lblTitulo.Text = "Iniciar sesión";
            //
            // lblSubtitulo
            //
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.Font = new Font("Segoe UI", 9.5F);
            lblSubtitulo.ForeColor = Color.FromArgb(0x6B, 0x72, 0x80);
            lblSubtitulo.BackColor = Color.Transparent;
            lblSubtitulo.Location = new Point(58, 92);
            lblSubtitulo.Text = "Ingresa tus credenciales para continuar";
            //
            // lblUsuario
            //
            lblUsuario.AutoSize = true;
            lblUsuario.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblUsuario.ForeColor = Color.FromArgb(0x37, 0x41, 0x51);
            lblUsuario.BackColor = Color.Transparent;
            lblUsuario.Location = new Point(58, 136);
            lblUsuario.Text = "Email o usuario";
            //
            // txtUsuario
            //
            txtUsuario.BorderStyle = BorderStyle.FixedSingle;
            txtUsuario.BackColor = Color.White;
            txtUsuario.Font = new Font("Segoe UI", 11F);
            txtUsuario.Location = new Point(58, 158);
            txtUsuario.Size = new Size(324, 27);
            txtUsuario.TabIndex = 0;
            //
            // lblPassword
            //
            lblPassword.AutoSize = true;
            lblPassword.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblPassword.ForeColor = Color.FromArgb(0x37, 0x41, 0x51);
            lblPassword.BackColor = Color.Transparent;
            lblPassword.Location = new Point(58, 204);
            lblPassword.Text = "Contraseña";
            //
            // txtPassword
            //
            txtPassword.BorderStyle = BorderStyle.FixedSingle;
            txtPassword.BackColor = Color.White;
            txtPassword.Font = new Font("Segoe UI", 11F);
            txtPassword.Location = new Point(58, 226);
            txtPassword.Size = new Size(324, 27);
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.TabIndex = 1;
            //
            // lblError
            //
            lblError.AutoSize = false;
            lblError.Font = new Font("Segoe UI", 9F);
            lblError.ForeColor = Color.FromArgb(0xB9, 0x1C, 0x1C);
            lblError.BackColor = Color.Transparent;
            lblError.Location = new Point(58, 262);
            lblError.Size = new Size(324, 20);
            lblError.Text = "";
            //
            // btnLogin
            //
            btnLogin.BackColor = Color.FromArgb(0x00, 0x4F, 0x3B);
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.FlatAppearance.MouseOverBackColor = Color.FromArgb(0x00, 0x3F, 0x2F);
            btnLogin.FlatAppearance.MouseDownBackColor = Color.FromArgb(0x00, 0x35, 0x28);
            btnLogin.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            btnLogin.ForeColor = Color.White;
            btnLogin.Cursor = Cursors.Hand;
            btnLogin.Location = new Point(58, 294);
            btnLogin.Size = new Size(324, 44);
            btnLogin.TabIndex = 2;
            btnLogin.Text = "Login";
            btnLogin.UseVisualStyleBackColor = false;
            btnLogin.Click += btnLogin_Click;
            //
            // Login
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(0xF9, 0xFA, 0xFB);
            ClientSize = new Size(900, 600);
            MinimumSize = new Size(520, 480);
            Controls.Add(card);
            AcceptButton = btnLogin;
            Name = "Login";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Login";
            Load += Login_Load;
            card.ResumeLayout(false);
            card.PerformLayout();
            ResumeLayout(false);
        }
        #endregion

        private CardPanel card;
        private Label lblTitulo;
        private Label lblSubtitulo;
        private Label lblUsuario;
        private TextBox txtUsuario;
        private Label lblPassword;
        private TextBox txtPassword;
        private Label lblError;
        private Button btnLogin;
    }
}