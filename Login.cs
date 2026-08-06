using System;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Tela de login do sistema.</summary>
    public partial class Login : Form
    {
        public Usuario UsuarioLogado { get; private set; }
        private TextBox txtUsuario;
        private TextBox txtSenha;
        private Button btnEntrar;

        public Login()
        {
            InitializeComponent();
            Text = "Soen - Entrar";
            ClientSize = new Size(340, 190);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = SystemColors.GradientInactiveCaption;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            CriarInterface();
        }

        private void CriarInterface()
        {
            var l1 = new Label { Text = "Usuário:", AutoSize = true, Location = new Point(20, 22) };
            txtUsuario = new TextBox { Location = new Point(90, 19), Size = new Size(200, 20) };

            var l2 = new Label { Text = "Senha:", AutoSize = true, Location = new Point(20, 52) };
            txtSenha = new TextBox { Location = new Point(90, 49), Size = new Size(200, 20), PasswordChar = '*' };

            btnEntrar = new Button { Text = "Entrar", Location = new Point(90, 90), Size = new Size(90, 30), BackColor = SystemColors.AppWorkspace };
            btnEntrar.Click += (s, e) => Entrar();
            btnEntrar.DialogResult = DialogResult.None;

            var btnCancelar = new Button { Text = "Cancelar", Location = new Point(200, 90), Size = new Size(90, 30) };
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            AcceptButton = btnEntrar;
            txtSenha.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Entrar(); };

            Controls.AddRange(new Control[] { l1, txtUsuario, l2, txtSenha, btnEntrar, btnCancelar });
        }

        private void Entrar()
        {
            var u = UsuarioDAO.Autenticar(txtUsuario.Text.Trim(), txtSenha.Text);
            if (u == null)
            {
                MessageBox.Show("Usuário ou senha inválidos.", "Acesso negado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            UsuarioLogado = u;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Login_Load(object sender, EventArgs e)
        {

        }
    }
}