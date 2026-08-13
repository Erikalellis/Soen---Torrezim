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
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = SystemColors.GradientInactiveCaption;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            CriarInterface();
        }

        private void CriarInterface()
        {
            Empresa emp = EmpresaDAO.Obter();
            string nomeEmpresa = string.IsNullOrWhiteSpace(emp.Nome) ? "Soen - Sistema de Ordem de Serviço" : emp.Nome;

            // Marca: logo (se configurado) + nome da empresa
            var logo = new PictureBox { Location = new Point(110, 18), Size = new Size(120, 70), SizeMode = PictureBoxSizeMode.Zoom };
            bool temLogo = false;
            try
            {
                if (!string.IsNullOrWhiteSpace(emp.LogoPath) && System.IO.File.Exists(emp.LogoPath))
                {
                    using (var img = Image.FromFile(emp.LogoPath))
                    {
                        logo.Image = new Bitmap(img);
                    }
                    temLogo = true;
                }
            }
            catch (Exception ex) { Logger.LogError(ex); }

            var lblTitulo = new Label
            {
                Text = nomeEmpresa,
                AutoSize = true,
                Location = new Point(20, temLogo ? 96 : 40),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            int yBase = temLogo ? 130 : 74;

            var l1 = new Label { Text = "Usuário:", AutoSize = true, Location = new Point(20, yBase + 22) };
            txtUsuario = new TextBox { Location = new Point(90, yBase + 19), Size = new Size(200, 20) };

            var l2 = new Label { Text = "Senha:", AutoSize = true, Location = new Point(20, yBase + 52) };
            txtSenha = new TextBox { Location = new Point(90, yBase + 49), Size = new Size(200, 20), PasswordChar = '*' };

            btnEntrar = UIHelpers.CreateButton("Entrar", new Point(90, yBase + 90), new Size(90, 30));
            btnEntrar.Click += (s, e) => Entrar();
            btnEntrar.DialogResult = DialogResult.None;

            var btnCancelar = UIHelpers.CreateButton("Cancelar", new Point(200, yBase + 90), new Size(90, 30));
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            ClientSize = new Size(340, temLogo ? 260 : 190);

            AcceptButton = btnEntrar;
            txtSenha.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Entrar(); };

            Controls.Add(lblTitulo);
            if (temLogo) Controls.Add(logo);
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
            if (u.TrocarSenha)
            {
                using (var troca = new TrocarSenha(u, obrigatoria: true))
                {
                    if (troca.ShowDialog() != DialogResult.OK)
                    {
                        MessageBox.Show("Você precisa definir uma nova senha antes de continuar.", "Atenção",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    u = UsuarioDAO.Autenticar(txtUsuario.Text.Trim(), txtSenha.Text);
                    if (u == null) return;
                }
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