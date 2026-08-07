using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Relatório de Fornecedores e Credores (fornecedores + contas a pagar).</summary>
    public partial class RelatorioFC : Form
    {
        private TabControl tabs;
        private DataGridView gridFornecedores;
        private DataGridView gridContas;
        private Button btnGerar;
        private Button btnExportar;
        private Button btnImprimir;

        public RelatorioFC()
        {
            InitializeComponent();
            Text = "Soen - Relatório: Fornecedores e Credores";
            ClientSize = new Size(820, 470);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
        }

        private void CriarInterface()
        {
            btnGerar = UIHelpers.CreateButton("Gerar Relatório", new Point(12, 12), new Size(130, 28));
            btnGerar.Click += (s, e) => Carregar();
            btnExportar = UIHelpers.CreateButton("Exportar CSV", new Point(150, 12), new Size(110, 28));
            btnExportar.Click += (s, e) => RelatorioHelper.ExportarCsv(GridAtivo(), "fornecedores_credores.csv");
            btnImprimir = UIHelpers.CreateButton("Imprimir", new Point(268, 12), new Size(100, 28));
            btnImprimir.Click += (s, e) => RelatorioHelper.Imprimir(GridAtivo(), "Soen - Fornecedores e Credores");

            tabs = new TabControl { Location = new Point(12, 50), Size = new Size(790, 380) };

            gridFornecedores = NovoGrid();
            gridFornecedores.Columns.Add("Nome", "Fornecedor");
            gridFornecedores.Columns.Add("Cnpj", "CNPJ/CPF");
            gridFornecedores.Columns.Add("Cidade", "Cidade");
            gridFornecedores.Columns.Add("Fone", "Telefone");
            tabs.TabPages.Add("Fornecedores");
            tabs.TabPages[0].Controls.Add(gridFornecedores);

            gridContas = NovoGrid();
            gridContas.Columns.Add("Descricao", "Descrição");
            gridContas.Columns.Add("Vencimento", "Vencimento");
            gridContas.Columns.Add("Valor", "Valor");
            gridContas.Columns.Add("Status", "Status");
            tabs.TabPages.Add("Contas a Pagar");
            tabs.TabPages[1].Controls.Add(gridContas);

            Controls.AddRange(new Control[] { btnGerar, btnExportar, btnImprimir, tabs });
        }

        private DataGridView GridAtivo()
        {
            return tabs.SelectedIndex == 1 ? gridContas : gridFornecedores;
        }

        private DataGridView NovoGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
        }

        private void Carregar()
        {
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            gridFornecedores.Rows.Clear();
            foreach (Cliente c in ClienteDAO.Listar(""))
                if (c.Tipo == "fornecedor")
                    gridFornecedores.Rows.Add(c.NomeRazao, c.CpfCnpj, c.Cidade, c.Fone1);

            gridContas.Rows.Clear();
            foreach (ContaFinanceira c in FinanceiroDAO.Listar("pagar"))
                gridContas.Rows.Add(c.Descricao, c.Vencimento, c.Valor.ToString("N2", cult), c.Status);
        }
    }
}