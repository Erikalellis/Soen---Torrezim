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
        private DateTimePicker dtpInicio;
        private DateTimePicker dtpFim;
        private Label lblVendas;
        private Label lblSaidas;
        private Label lblAReceber;
        private Label lblAPagar;
        private Label lblLucro;

        public AnaliseLucro()
        {
            InitializeComponent();
            Text = "Soen - Análise de Margem de Lucro";
            ClientSize = new Size(940, 540);
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

            // Filtro por período (lado direito, acima do gráfico)
            var lIni = new Label { Text = "De:", AutoSize = true, Location = new Point(420, 16) };
            dtpInicio = new DateTimePicker
            {
                Location = new Point(452, 13),
                Size = new Size(110, 20),
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
            };
            var lFim = new Label { Text = "Até:", AutoSize = true, Location = new Point(580, 16) };
            dtpFim = new DateTimePicker
            {
                Location = new Point(612, 13),
                Size = new Size(110, 20),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            var lPeriodo = new Label
            {
                Text = "Período analisado (vendas, caixa e vencimentos)",
                AutoSize = true,
                Location = new Point(420, 44),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = SystemColors.GrayText
            };

            chart = new Chart { Location = new Point(420, 68), Size = new Size(360, 230), BackColor = Color.Transparent };
            chart.ChartAreas.Add(new ChartArea());
            chart.ChartAreas[0].AxisX.Title = "Indicador";
            chart.ChartAreas[0].AxisY.Title = "R$";
            chart.Series.Add(new Series { ChartType = SeriesChartType.Column, LegendText = "Indicadores" });
            chart.Size = new Size(360, 220);

            grid = new DataGridView
            {
                Location = new Point(12, 190),
                Size = new Size(912, 330),
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            grid.Columns.Add("Indicador", "Indicador");
            grid.Columns.Add("Valor", "Valor");

            Controls.AddRange(new Control[] { lblVendas, lblSaidas, lblAReceber, lblAPagar, lblLucro, btnRecalcular,
                lIni, dtpInicio, lFim, dtpFim, lPeriodo, chart, grid });
        }

        private Label NovoLbl(int y)
        {
            return new Label { AutoSize = true, Location = new Point(12, y), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        }

        private void Recalcular()
        {
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            DateTime ini = dtpInicio.Value.Date;
            DateTime fim = dtpFim.Value.Date;

            double vendas = 0, saidas = 0;
            foreach (Venda v in VendaDAO.Listar(null))
                if (Dentro(v.Data, ini, fim)) vendas += v.ValorTotal;
            foreach (LancamentoCaixa l in CaixaDAO.Listar())
                if (Dentro(l.Data, ini, fim) && l.Tipo == "saida") saidas += l.Valor;

            double aReceber = 0, aPagar = 0;
            foreach (ContaFinanceira c in FinanceiroDAO.Listar("receber"))
                if (EmAbertoNoPeriodo(c, ini, fim)) aReceber += c.Valor;
            foreach (ContaFinanceira c in FinanceiroDAO.Listar("pagar"))
                if (EmAbertoNoPeriodo(c, ini, fim)) aPagar += c.Valor;

            lblVendas.Text = "Total de Vendas:          " + vendas.ToString("N2", cult);
            lblSaidas.Text = "Despesas (saídas caixa):  " + saidas.ToString("N2", cult);
            lblAReceber.Text = "Contas a Receber (aberto):" + aReceber.ToString("N2", cult);
            lblAPagar.Text = "Contas a Pagar (aberto):  " + aPagar.ToString("N2", cult);

            double lucro = vendas - saidas;
            lblLucro.Text = "Margem / Lucro estimado:  " + lucro.ToString("N2", cult);

            grid.Rows.Clear();
            grid.Rows.Add("Período", ini.ToString("dd/MM/yyyy") + " até " + fim.ToString("dd/MM/yyyy"));
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

        private static bool Dentro(string dataStr, DateTime ini, DateTime fim)
        {
            DateTime d;
            return !string.IsNullOrWhiteSpace(dataStr) && DateTime.TryParse(dataStr, out d) &&
                   d.Date >= ini && d.Date <= fim;
        }

        private static bool EmAbertoNoPeriodo(ContaFinanceira c, DateTime ini, DateTime fim)
        {
            if (c.Status == "pago" || c.Status == "cancelado") return false;
            DateTime d;
            return !string.IsNullOrWhiteSpace(c.Vencimento) && DateTime.TryParse(c.Vencimento, out d) &&
                   d.Date >= ini && d.Date <= fim;
        }
    }
}