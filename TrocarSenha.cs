using System;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>
    /// Troca de senha do usuário. Quando <see cref="Obrigatoria"/> é true, o usuário
    /// não consegue prosseguir no sistema sem definir uma nova senha (senha padrão).
    /// </summary>
    public partial class TrocarSenha : Form
    {
        private readonly Usuario _usuario;
        private readonly bool _obrigatoria;
        private TextBox txtAtual;
        private TextBox txtNova;
        private TextBox txtConfirma;
        private Label lblAviso;

        public TrocarSenha(Usuario usuario, bool obrigatoria = false)
        {
            _usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
            _obrigatoria = obrigatoria;
            InitializeComponent();
            Text = "Soen - Trocar Senha";
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            CriarInterface();
        }

        private void CriarInterface()
        {
            var lblUsuario = new Label
            {
                Text = "Usuário: " + _usuario.Nome,
                AutoSize = true,
                Location = new Point(16, 16),
                Font = new Font("Segoe UI", 9.75f, FontStyle.Bold)
            };

            txtAtual = new TextBox { Location = new Point(140, 48), Size = new Size(180, 20), PasswordChar = '*' };
            var lAtual = new Label { Text = "Senha atual:", AutoSize = true, Location = new Point(16, 51), Width = 120 };

            txtNova = new TextBox { Location = new Point(140, 78), Size = new Size(180, 20), PasswordChar = '*' };
            var lNova = new Label { Text = "Nova senha:", AutoSize = true, Location = new Point(16, 81), Width = 120 };

            txtConfirma = new TextBox { Location = new Point(140, 108), Size = new Size(180, 20), PasswordChar = '*' };
            var lConfirma = new Label { Text = "Confirmar:", AutoSize = true, Location = new Point(16, 111), Width = 120 };

            lblAviso = new Label
            {
                Text = "A senha deve ter ao menos " + UsuarioDAO.SenhaMinima + " caracteres.",
                AutoSize = true,
                Location = new Point(16, 136),
                ForeColor = Color.Gray,
                Width = 300
            };

            if (_obrigatoria)
            {
                var lblObrigatorio = new Label
                {
                    Text = "Você está usando uma senha provisória e deve definir uma nova antes de continuar.",
                    AutoSize = true,
                    Location = new Point(16, 36),
                    ForeColor = Color.Maroon,
                    Width = 304
                };
                Controls.Add(lblObrigatorio);
            }

            var btnSalvar = UIHelpers.CreateButton("Salvar", new Point(140, 162), new Size(90, 30));
            btnSalvar.Click += (s, e) => Salvar();

            var btnCancelar = UIHelpers.CreateButton("Cancelar", new Point(240, 162), new Size(90, 30));
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            ClientSize = new Size(345, 210);
            AcceptButton = btnSalvar;
            CancelButton = btnCancelar;
            txtNova.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Salvar(); };

            Controls.AddRange(new Control[] { lblUsuario, lAtual, txtAtual, lNova, txtNova, lConfirma, txtConfirma, lblAviso, btnSalvar, btnCancelar });
        }

        private void Salvar()
        {
            string atual = txtAtual.Text ?? "";
            string nova = txtNova.Text ?? "";
            string confirma = txtConfirma.Text ?? "";

            Usuario check = UsuarioDAO.Autenticar(_usuario.Login, atual);
            if (check == null || check.Id != _usuario.Id)
            {
                MessageBox.Show("A senha atual está incorreta.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAtual.Focus();
                return;
            }
            if (!UsuarioDAO.SenhaForte(nova))
            {
                MessageBox.Show("A nova senha deve ter ao menos " + UsuarioDAO.SenhaMinima + " caracteres.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNova.Focus();
                return;
            }
            if (nova != confirma)
            {
                MessageBox.Show("A confirmação não confere com a nova senha.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtConfirma.Focus();
                return;
            }

            try
            {
                UsuarioDAO.AtualizarSenha(_usuario.Id, nova);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                MessageBox.Show("Não foi possível alterar a senha: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}