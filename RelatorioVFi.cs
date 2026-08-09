using System;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Relatório de Vendas e Financeiro (vendas, caixa e contas).</summary>
    public partial class RelatorioVFi : BaseForm
    {
        private TabControl tabs;
        private DataGridView gridVendas;
        private DataGridView gridCaixa;
        private DataGridView gridContas;
        private Label lblTotais;
        private Button btnGerar;
        private Button btnExportar;
        private Button btnImprimir;

        public RelatorioVFi()
        {
            InitializeComponent();
            Text = "Soen - Relatório: Vendas e Financeiro";
            ClientSize = new Size(840, 480);
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
            btnExportar.Click += (s, e) => RelatorioHelper.ExportarCsv(GridAtivo(), "vendas_financeiro.csv");
            btnImprimir = UIHelpers.CreateButton("Imprimir", new Point(268, 12), new Size(100, 28));
            btnImprimir.Click += (s, e) => RelatorioHelper.Imprimir(GridAtivo(), "Soen - Vendas e Financeiro");
            RelatorioHelper.AdicionarBotoesExportar(this, GridAtivo, "Soen - Vendas e Financeiro", "vendas_financeiro", 376, 12);
            lblTotais = new Label { Text = "", AutoSize = true, Location = new Point(12, 48), Font = new Font("Segoe UI", 9, FontStyle.Bold) };

            tabs = new TabControl { Location = new Point(12, 74), Size = new Size(810, 380) };

            gridVendas = NovoGrid();
            gridVendas.Columns.Add("Data", "Data");
            gridVendas.Columns.Add("Cliente", "Cliente");
            gridVendas.Columns.Add("Valor", "Valor");
            gridVendas.Columns.Add("Forma", "Pagamento");
            tabs.TabPages.Add("Vendas");
            tabs.TabPages[0].Controls.Add(gridVendas);

            gridCaixa = NovoGrid();
            gridCaixa.Columns.Add("Data", "Data");
            gridCaixa.Columns.Add("Descricao", "Descrição");
            gridCaixa.Columns.Add("Tipo", "Tipo");
            gridCaixa.Columns.Add("Valor", "Valor");
            tabs.TabPages.Add("Caixa");
            tabs.TabPages[1].Controls.Add(gridCaixa);

            gridContas = NovoGrid();
            gridContas.Columns.Add("Tipo", "Tipo");
            gridContas.Columns.Add("Descricao", "Descrição");
            gridContas.Columns.Add("Vencimento", "Vencimento");
            gridContas.Columns.Add("Valor", "Valor");
            gridContas.Columns.Add("Status", "Status");
            tabs.TabPages.Add("Contas");
            tabs.TabPages[2].Controls.Add(gridContas);

            Controls.AddRange(new Control[] { btnGerar, btnExportar, btnImprimir, lblTotais, tabs });
        }

        private DataGridView GridAtivo()
        {
            if (tabs.SelectedIndex == 1) return gridCaixa;
            if (tabs.SelectedIndex == 2) return gridContas;
            return gridVendas;
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
            gridVendas.Rows.Clear();
            gridCaixa.Rows.Clear();
            gridContas.Rows.Clear();

            double totVendas = 0;
            foreach (Venda v in VendaDAO.Listar(null))
            {
                totVendas += v.ValorTotal;
                gridVendas.Rows.Add(v.Data, v.NomeCliente, v.ValorTotal.ToString("N2", cult), v.FormaPagamento);
            }
            foreach (LancamentoCaixa l in CaixaDAO.Listar())
                gridCaixa.Rows.Add(l.Data, l.Descricao, l.Tipo, l.Valor.ToString("N2", cult));
            foreach (ContaFinanceira c in FinanceiroDAO.Listar(""))
                gridContas.Rows.Add(c.Tipo, c.Descricao, c.Vencimento, c.Valor.ToString("N2", cult), c.Status);

            lblTotais.Text = "Total de vendas: " + totVendas.ToString("N2", cult) +
                "  |  Saldo caixa: " + CaixaDAO.Saldo().ToString("N2", cult) +
                "  |  A pagar: " + FinanceiroDAO.TotalAberto("pagar").ToString("N2", cult) +
                "  |  A receber: " + FinanceiroDAO.TotalAberto("receber").ToString("N2", cult);
        }
    }
}