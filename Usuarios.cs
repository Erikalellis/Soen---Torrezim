using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Gerenciamento de usuários do sistema.</summary>
    public partial class Usuarios : BaseForm
    {
        private TextBox txtUsuario;
        private TextBox txtNome;
        private TextBox txtSenha;
        private ComboBox cmbPerfil;
        private CheckBox chkAtivo;
        private Button btnSalvar;
        private Button btnExcluir;
        private DataGridView grid;

        public Usuarios()
        {
            InitializeComponent();
            Text = "Soen - Usuários";
            ClientSize = new Size(620, 460);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
        }

        private void CriarInterface()
        {
            var l1 = new Label { Text = "Usuário:", AutoSize = true, Location = new Point(12, 20) };
            txtUsuario = new TextBox { Location = new Point(90, 17), Size = new Size(150, 20) };
            var l2 = new Label { Text = "Nome:", AutoSize = true, Location = new Point(260, 20) };
            txtNome = new TextBox { Location = new Point(305, 17), Size = new Size(280, 20) };

            var l3 = new Label { Text = "Senha:", AutoSize = true, Location = new Point(12, 50) };
            txtSenha = new TextBox { Location = new Point(80, 47), Size = new Size(150, 20), PasswordChar = '*' };
            var l4 = new Label { Text = "Perfil:", AutoSize = true, Location = new Point(260, 50) };
            cmbPerfil = new ComboBox { Location = new Point(305, 47), Size = new Size(140, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPerfil.Items.Add("operador");
            cmbPerfil.Items.Add("admin");
            cmbPerfil.SelectedIndex = 0;
            chkAtivo = new CheckBox { Text = "Ativo", Checked = true, Location = new Point(470, 48) };

            btnSalvar = new Button { Text = "Salvar", Location = new Point(12, 110), Size = new Size(90, 28), BackColor = SystemColors.AppWorkspace };
            btnSalvar.Click += (s, e) => Salvar();
            btnExcluir = new Button { Text = "Excluir Sel.", Location = new Point(120, 110), Size = new Size(100, 28), BackColor = SystemColors.AppWorkspace };
            btnExcluir.Click += (s, e) => Excluir();

            grid = new DataGridView
            {
                Location = new Point(12, 150),
                Size = new Size(595, 280),
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            grid.Columns.Add("Id", "Nº");
            grid.Columns.Add("Usuario", "Usuário");
            grid.Columns.Add("Nome", "Nome");
            grid.Columns.Add("Perfil", "Perfil");
            grid.Columns.Add("Ativo", "Ativo");
            grid.SelectionChanged += (s, e) => Preencher();

            Controls.AddRange(new Control[] { l1, txtUsuario, l2, txtNome, l3, txtSenha, l4, cmbPerfil, chkAtivo, btnSalvar, btnExcluir, grid });
        }

        private void Carregar()
        {
            grid.Rows.Clear();
            foreach (Usuario u in UsuarioDAO.Listar())
                grid.Rows.Add(u.Id, u.Login, u.Nome, u.Perfil, u.Ativo ? "Sim" : "Não");
        }

        private long? SelecionadoId()
        {
            if (grid.SelectedRows.Count == 0) return null;
            return Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
        }

        private void Preencher()
        {
            var id = SelecionadoId();
            if (!id.HasValue) return;
            foreach (Usuario u in UsuarioDAO.Listar())
            {
                if (u.Id == id.Value)
                {
                    txtUsuario.Text = u.Login;
                    txtNome.Text = u.Nome;
                    cmbPerfil.SelectedItem = u.Perfil == "admin" ? "admin" : "operador";
                    chkAtivo.Checked = u.Ativo;
                    txtSenha.Clear();
                    return;
                }
            }
        }

        private void Salvar()
        {
            if (string.IsNullOrWhiteSpace(txtUsuario.Text))
            {
                MessageBox.Show("Informe o nome de usuário.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var id = SelecionadoId();
            var u = id.HasValue ? new Usuario { Id = id.Value } : new Usuario();
            u.Login = txtUsuario.Text.Trim();
            u.Nome = txtNome.Text.Trim();
            u.Perfil = cmbPerfil.SelectedItem?.ToString() ?? "operador";
            u.Ativo = chkAtivo.Checked;
            if (u.Id == 0)
            {
                u.Senha = string.IsNullOrWhiteSpace(txtSenha.Text) ? "1234" : txtSenha.Text;
                using (var conn = Soen___Torrezim.Data.Database.AbrirConexao())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM usuarios WHERE usuario=@u";
                    cmd.Parameters.AddWithValue("@u", u.Login);
                    if ((long)cmd.ExecuteScalar() > 0)
                    {
                        MessageBox.Show("Já existe um usuário com esse nome.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                UsuarioDAO.Salvar(u);
            }
            else
            {
                UsuarioDAO.Salvar(u);
                if (!string.IsNullOrWhiteSpace(txtSenha.Text))
                    UsuarioDAO.AtualizarSenha(u.Id, txtSenha.Text);
            }
            Carregar();
            Limpar();
        }

        private void Excluir()
        {
            var id = SelecionadoId();
            if (!id.HasValue) return;
            if (MessageBox.Show("Excluir este usuário?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                UsuarioDAO.Excluir(id.Value);
                Carregar();
                Limpar();
            }
        }

        private void Limpar()
        {
            txtUsuario.Clear();
            txtNome.Clear();
            txtSenha.Clear();
            cmbPerfil.SelectedIndex = 0;
            chkAtivo.Checked = true;
            grid.ClearSelection();
        }
    }
}