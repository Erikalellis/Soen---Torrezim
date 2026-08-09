using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Cadastro de Fornecedores (usa a tabela clientes com tipo = 'fornecedor').</summary>
    public partial class CadastroFornecedor : BaseForm
    {
        private TextBox txtNome;
        private TextBox txtCpfCnpj;
        private TextBox txtEndereco;
        private TextBox txtFone;
        private TextBox txtEmail;
        private TextBox txtResponsavel;
        private Button btnSalvar;
        private Button btnExcluir;
        private Button btnLimpar;
        private DataGridView grid;

        public CadastroFornecedor()
        {
            InitializeComponent();
            Text = "Soen - Fornecedores";
            ClientSize = new Size(720, 480);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
            txtCpfCnpj.TextChanged += (s, e) => Validacoes.AplicarMascaraCpfCnpj(txtCpfCnpj);
            txtFone.TextChanged += (s, e) => Validacoes.AplicarMascaraTelefone(txtFone);
        }

        private void CriarInterface()
        {
            var l1 = new Label { Text = "Nome/Razão:", AutoSize = true, Location = new Point(12, 20) };
            txtNome = new TextBox { Location = new Point(100, 17), Size = new Size(330, 20) };

            var l2 = new Label { Text = "CNPJ/CPF:", AutoSize = true, Location = new Point(12, 50) };
            txtCpfCnpj = new TextBox { Location = new Point(100, 47), Size = new Size(180, 20) };

            var l3 = new Label { Text = "Endereço:", AutoSize = true, Location = new Point(12, 80) };
            txtEndereco = new TextBox { Location = new Point(100, 77), Size = new Size(380, 20) };

            var l4 = new Label { Text = "Telefone:", AutoSize = true, Location = new Point(12, 110) };
            txtFone = new TextBox { Location = new Point(100, 107), Size = new Size(150, 20) };

            var l5 = new Label { Text = "Email:", AutoSize = true, Location = new Point(300, 110) };
            txtEmail = new TextBox { Location = new Point(345, 107), Size = new Size(200, 20) };

            var l6 = new Label { Text = "Responsável:", AutoSize = true, Location = new Point(12, 140) };
            txtResponsavel = new TextBox { Location = new Point(100, 137), Size = new Size(200, 20) };

            btnSalvar = UIHelpers.CreateButton("Salvar", new Point(12, 170), new Size(100, 28));
            btnSalvar.Click += (s, e) => Salvar();
            btnLimpar = UIHelpers.CreateButton("Limpar", new Point(120, 170), new Size(100, 28));
            btnLimpar.Click += (s, e) => LimparCampos();
            btnExcluir = UIHelpers.CreateButton("Excluir Sel.", new Point(228, 170), new Size(110, 28));
            btnExcluir.Click += (s, e) => Excluir();

            grid = new DataGridView
            {
                Location = new Point(12, 210),
                Size = new Size(700, 240),
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            grid.Columns.Add("Id", "Nº");
            grid.Columns.Add("Nome", "Nome/Razão");
            grid.Columns.Add("CpfCnpj", "CNPJ/CPF");
            grid.Columns.Add("Endereco", "Endereço");
            grid.Columns.Add("Fone", "Telefone");
            grid.Columns.Add("Responsavel", "Responsável");

            Controls.AddRange(new Control[] { l1, txtNome, l2, txtCpfCnpj, l3, txtEndereco, l4, txtFone, l5, txtEmail, l6, txtResponsavel,
                btnSalvar, btnLimpar, btnExcluir, grid });
        }

        private void Carregar()
        {
            grid.Rows.Clear();
            List<Cliente> lista = ClienteDAO.Listar("");
            foreach (var c in lista)
            {
                if (c.Tipo == "fornecedor")
                    grid.Rows.Add(c.Id, c.NomeRazao, c.CpfCnpj, c.Endereco, c.Fone1, c.Responsavel);
            }
        }

        private void Salvar()
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show("Informe o nome/razão do fornecedor.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string cpfCnpj = Validacoes.SoDigitos(txtCpfCnpj.Text);
            if (cpfCnpj.Length > 0)
            {
                bool valido = cpfCnpj.Length == 11 ? Validacoes.CpfValido(cpfCnpj)
                                                   : cpfCnpj.Length == 14 && Validacoes.CnpjValido(cpfCnpj);
                if (!valido)
                {
                    MessageBox.Show("CNPJ/CPF inválido.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCpfCnpj.Focus();
                    return;
                }
            }
            var c = Selecionado() ?? new Cliente();
            c.Tipo = "fornecedor";
            c.NomeRazao = txtNome.Text.Trim();
            c.CpfCnpj = txtCpfCnpj.Text.Trim();
            c.Endereco = txtEndereco.Text.Trim();
            c.Fone1 = txtFone.Text.Trim();
            c.Email1 = txtEmail.Text.Trim();
            c.Responsavel = txtResponsavel.Text.Trim();
            ClienteDAO.Salvar(c);
            Carregar();
            LimparCampos();
            MessageBox.Show("Fornecedor salvo!", "SOEN", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private Cliente Selecionado()
        {
            if (grid.SelectedRows.Count == 0) return null;
            var row = grid.SelectedRows[0];
            var c = new Cliente { Id = Convert.ToInt64(row.Cells["Id"].Value) };
            txtNome.Text = row.Cells["Nome"].Value?.ToString() ?? "";
            txtCpfCnpj.Text = row.Cells["CpfCnpj"].Value?.ToString() ?? "";
            txtEndereco.Text = row.Cells["Endereco"].Value?.ToString() ?? "";
            txtFone.Text = row.Cells["Fone"].Value?.ToString() ?? "";
            txtResponsavel.Text = row.Cells["Responsavel"].Value?.ToString() ?? "";
            return c;
        }

        private void Excluir()
        {
            if (grid.SelectedRows.Count == 0) return;
            long id = Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
            if (MessageBox.Show("Excluir este fornecedor?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ClienteDAO.Excluir(id);
                Carregar();
                LimparCampos();
            }
        }

        private void LimparCampos()
        {
            txtNome.Clear();
            txtCpfCnpj.Clear();
            txtEndereco.Clear();
            txtEmail.Clear();
            txtFone.Clear();
            txtResponsavel.Clear();
        }
    }
}