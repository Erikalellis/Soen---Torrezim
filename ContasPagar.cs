using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Contas a Pagar (módulo financeiro).</summary>
    public partial class ContasPagar : Form
    {
        private TextBox txtDescricao;
        private TextBox txtFornecedor;
        private DateTimePicker dtpVencimento;
        private TextBox txtValor;
        private Button btnAdicionar;
        private Button btnPagar;
        private Button btnExcluir;
        private DataGridView grid;
        private StatusStrip statusBar;
        private ToolStripStatusLabel lblStatus;

        public ContasPagar()
        {
            InitializeComponent();
            Text = "Soen - Contas a Pagar";
            ClientSize = new Size(820, 460);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
            txtValor.TextChanged += (s, e) => Validacoes.AplicarMascaraValor(txtValor);
        }

        private void CriarInterface()
        {
            var l1 = new Label { Text = "Descrição:", AutoSize = true, Location = new Point(12, 20) };
            txtDescricao = new TextBox { Location = new Point(90, 17), Size = new Size(300, 20) };

            var l2 = new Label { Text = "Fornecedor:", AutoSize = true, Location = new Point(400, 20) };
            txtFornecedor = new TextBox { Location = new Point(480, 17), Size = new Size(200, 20) };

            var l3 = new Label { Text = "Vencimento:", AutoSize = true, Location = new Point(12, 50) };
            dtpVencimento = new DateTimePicker { Location = new Point(90, 47), Size = new Size(110, 20), Format = DateTimePickerFormat.Short };

            var l4 = new Label { Text = "Valor (R$):", AutoSize = true, Location = new Point(230, 50) };
            txtValor = new TextBox { Location = new Point(310, 47), Size = new Size(120, 20) };

            btnAdicionar = UIHelpers.CreateButton("Adicionar Conta", new Point(12, 90), new Size(130, 28));
            btnAdicionar.Click += (s, e) => SalvarConta(false);
            btnPagar = UIHelpers.CreateButton("Marcar Pago Sel.", new Point(152, 90), new Size(130, 28));
            btnPagar.Click += (s, e) => AlterarStatus("pago");
            btnExcluir = UIHelpers.CreateButton("Excluir Sel.", new Point(292, 90), new Size(110, 28));
            btnExcluir.Click += (s, e) => Excluir();

            grid = new DataGridView
            {
                Location = new Point(12, 132),
                Size = new Size(790, 330),
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            grid.Columns.Add("Id", "Nº");
            grid.Columns.Add("Descricao", "Descrição");
            grid.Columns.Add("Fornecedor", "Fornecedor");
            grid.Columns.Add("Vencimento", "Vencimento");
            grid.Columns.Add("Valor", "Valor");
            grid.Columns.Add("Status", "Status");

            statusBar = new StatusStrip();
            lblStatus = new ToolStripStatusLabel(" ");
            statusBar.Items.Add(lblStatus);
            statusBar.Location = new Point(0, 478);

            Controls.AddRange(new Control[] { l1, txtDescricao, l2, txtFornecedor, l3, dtpVencimento, l4, txtValor,
                btnAdicionar, btnPagar, btnExcluir, grid, statusBar });
        }

        private void Carregar()
        {
            grid.Rows.Clear();
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            List<ContaFinanceira> lista = FinanceiroDAO.Listar("pagar");
            foreach (var c in lista)
                grid.Rows.Add(c.Id, c.Descricao, c.Fornecedor, c.Vencimento, c.Valor.ToString("N2", cult), c.Status);
            lblStatus.Text = lista.Count + " conta(s) a pagar - Em aberto: R$ " +
                FinanceiroDAO.TotalAberto("pagar").ToString("N2", cult);
        }

        private void SalvarConta(bool status)
        {
            double valor;
            double.TryParse(txtValor.Text, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out valor);
            var c = new ContaFinanceira
            {
                Tipo = "pagar",
                Descricao = txtDescricao.Text.Trim(),
                Fornecedor = txtFornecedor.Text.Trim(),
                Vencimento = dtpVencimento.Value.ToString("yyyy-MM-dd"),
                Valor = valor
            };
            FinanceiroDAO.Salvar(c);
            Carregar();
            txtDescricao.Clear();
            txtFornecedor.Clear();
            txtValor.Clear();
        }

        private void AlterarStatus(string status)
        {
            var id = ContaSelecionada();
            if (!id.HasValue) return;
            List<ContaFinanceira> lista = FinanceiroDAO.Listar("pagar");
            foreach (var c in lista)
            {
                if (c.Id == id.Value) { c.Status = status; FinanceiroDAO.Salvar(c); break; }
            }
            Carregar();
        }

        private long? ContaSelecionada()
        {
            if (grid.SelectedRows.Count == 0) return null;
            return Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
        }

        private void Excluir()
        {
            var id = ContaSelecionada();
            if (!id.HasValue) return;
            if (MessageBox.Show("Excluir esta conta?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                FinanceiroDAO.Excluir(id.Value);
                Carregar();
            }
        }
    }
}