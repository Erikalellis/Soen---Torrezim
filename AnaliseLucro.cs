using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Análise de Margem de Lucro a partir das vendas, caixa e contas.</summary>
    public partial class AnaliseLucro : Form
    {
        private DataGridView grid;
        private Chart chart;
        private Button btnRecalcular;
        private Label lblVendas;
        private Label lblSaidas;
        private Label lblAReceber;
        private Label lblAPagar;
        private Label lblLucro;

        public AnaliseLucro()
        {
            InitializeComponent();
            Text = "Soen - Análise de Margem de Lucro";
            ClientSize = new Size(940, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Recalcular();
        }

        private void CriarInterface()
        {
            int y = 12;
            lblVendas = NovoLbl(y); y += 26;
            lblSaidas = NovoLbl(y); y += 26;
            lblAReceber = NovoLbl(y); y += 26;
            lblAPagar = NovoLbl(y); y += 26;
            lblLucro = NovoLbl(y);

            btnRecalcular = UIHelpers.CreateButton("Recalcular", new Point(12, 150), new Size(120, 28));
            btnRecalcular.Click += (s, e) => Recalcular();

            chart = new Chart { Location = new Point(400, 12), Size = new Size(340, 230), BackColor = Color.Transparent };
            chart.ChartAreas.Add(new ChartArea());
            chart.ChartAreas[0].AxisX.Title = "Indicador";
            chart.ChartAreas[0].AxisY.Title = "R$";
            chart.Series.Add(new Series { ChartType = SeriesChartType.Column, LegendText = "Indicadores" });
            chart.Size = new Size(340, 210);

            grid = new DataGridView
            {
                Location = new Point(12, 190),
                Size = new Size(720, 300),
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            grid.Columns.Add("Indicador", "Indicador");
            grid.Columns.Add("Valor", "Valor");

            Controls.AddRange(new Control[] { lblVendas, lblSaidas, lblAReceber, lblAPagar, lblLucro, btnRecalcular, chart, grid });
        }

        private Label NovoLbl(int y)
        {
            return new Label { AutoSize = true, Location = new Point(12, y), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        }

        private void Recalcular()
        {
            var cult = CultureInfo.GetCultureInfo("pt-BR");

            double vendas = 0, saidas = 0;
            foreach (Venda v in VendaDAO.Listar(null)) vendas += v.ValorTotal;
            foreach (LancamentoCaixa l in CaixaDAO.Listar())
                if (l.Tipo == "saida") saidas += l.Valor;

            double aReceber = FinanceiroDAO.TotalAberto("receber");
            double aPagar = FinanceiroDAO.TotalAberto("pagar");

            lblVendas.Text = "Total de Vendas:          " + vendas.ToString("N2", cult);
            lblSaidas.Text = "Despesas (saídas caixa):  " + saidas.ToString("N2", cult);
            lblAReceber.Text = "Contas a Receber (aberto):" + aReceber.ToString("N2", cult);
            lblAPagar.Text = "Contas a Pagar (aberto):  " + aPagar.ToString("N2", cult);

            double lucro = vendas - saidas;
            lblLucro.Text = "Margem / Lucro estimado:  " + lucro.ToString("N2", cult);

            grid.Rows.Clear();
            grid.Rows.Add("Receitas (vendas)", vendas.ToString("N2", cult));
            grid.Rows.Add("Despesas (saídas)", saidas.ToString("N2", cult));
            grid.Rows.Add("A receber (aberto)", aReceber.ToString("N2", cult));
            grid.Rows.Add("A pagar (aberto)", aPagar.ToString("N2", cult));
            grid.Rows.Add("LUCRO ESTIMADO", lucro.ToString("N2", cult));

            chart.Series[0].Points.Clear();
            chart.Series[0].Points.AddXY("Vendas", vendas);
            chart.Series[0].Points.AddXY("Saídas", saidas);
            chart.Series[0].Points.AddXY("A Receber", aReceber);
            chart.Series[0].Points.AddXY("A Pagar", aPagar);
            chart.Series[0].Points.AddXY("Lucro", lucro);
        }
    }
}