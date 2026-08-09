using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Cadastro do catálogo de Serviços (nome, descrição e preço).</summary>
    public partial class CadastroServico : BaseForm
    {
        private TextBox txtNome;
        private TextBox txtDescricao;
        private TextBox txtPreco;
        private Button btnSalvar;
        private Button btnLimpar;
        private Button btnExcluir;
        private DataGridView grid;

        public CadastroServico()
        {
            InitializeComponent();
            Text = "Soen - Catálogo de Serviços";
            ClientSize = new Size(640, 460);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
            txtPreco.TextChanged += (s, e) => Validacoes.AplicarMascaraValor(txtPreco);
        }

        private void CriarInterface()
        {
            var l1 = new Label { Text = "Serviço:", AutoSize = true, Location = new Point(12, 20) };
            txtNome = new TextBox { Location = new Point(80, 17), Size = new Size(330, 20) };

            var l2 = new Label { Text = "Descrição:", AutoSize = true, Location = new Point(12, 50) };
            txtDescricao = new TextBox { Location = new Point(80, 47), Size = new Size(500, 20) };

            var l3 = new Label { Text = "Preço (R$):", AutoSize = true, Location = new Point(12, 80) };
            txtPreco = new TextBox { Location = new Point(80, 77), Size = new Size(120, 20) };

            btnSalvar = UIHelpers.CreateButton("Salvar", new Point(12, 110), new Size(90, 28));
            btnSalvar.Click += (s, e) => Salvar();
            btnLimpar = UIHelpers.CreateButton("Limpar", new Point(110, 110), new Size(90, 28));
            btnLimpar.Click += (s, e) => LimparCampos();
            btnExcluir = UIHelpers.CreateButton("Excluir Sel.", new Point(208, 110), new Size(100, 28));
            btnExcluir.Click += (s, e) => Excluir();

            grid = new DataGridView
            {
                Location = new Point(12, 150),
                Size = new Size(610, 280),
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            grid.Columns.Add("Id", "Nº");
            grid.Columns.Add("Nome", "Serviço");
            grid.Columns.Add("Descricao", "Descrição");
            grid.Columns.Add("Preco", "Preço");
            grid.Columns["Descricao"].FillWeight = 2f;
            grid.SelectionChanged += (s, e) => PreencherCampos();

            Controls.AddRange(new Control[] { l1, txtNome, l2, txtDescricao, l3, txtPreco, btnSalvar, btnLimpar, btnExcluir, grid });
        }

        private void Carregar()
        {
            grid.Rows.Clear();
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            foreach (Servico sv in ServicoDAO.ListarServicos())
                grid.Rows.Add(sv.Id, sv.Nome, sv.Descricao, sv.Preco.ToString("N2", cult));
        }

        private void PreencherCampos()
        {
            if (grid.SelectedRows.Count == 0) return;
            var row = grid.SelectedRows[0];
            txtNome.Text = row.Cells["Nome"].Value?.ToString() ?? "";
            txtDescricao.Text = row.Cells["Descricao"].Value?.ToString() ?? "";
            txtPreco.Text = row.Cells["Preco"].Value?.ToString() ?? "";
        }

        private void Salvar()
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show("Informe o nome do serviço.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            double preco;
            double.TryParse(txtPreco.Text, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out preco);
            var sv = SelecionadoId() > 0 ? new Servico { Id = SelecionadoId() } : new Servico();
            sv.Nome = txtNome.Text.Trim();
            sv.Descricao = txtDescricao.Text.Trim();
            sv.Preco = preco;
            ServicoDAO.SalvarServico(sv);
            Carregar();
            LimparCampos();
        }

        private long SelecionadoId()
        {
            if (grid.SelectedRows.Count == 0) return 0;
            return Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
        }

        private void Excluir()
        {
            long id = SelecionadoId();
            if (id <= 0) return;
            if (MessageBox.Show("Excluir este serviço?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ServicoDAO.ExcluirServico(id);
                Carregar();
                LimparCampos();
            }
        }

        private void LimparCampos()
        {
            txtNome.Clear();
            txtDescricao.Clear();
            txtPreco.Clear();
            grid.ClearSelection();
        }
    }
}