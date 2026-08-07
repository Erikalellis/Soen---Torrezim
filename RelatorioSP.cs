using System;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Relatório de Serviços Prestados (agendamentos concluídos + orçamentos convertidos).</summary>
    public partial class RelatorioSP : Form
    {
        private DataGridView grid;
        private Label lblTotal;
        private Button btnGerar;
        private Button btnExportar;
        private Button btnImprimir;

        public RelatorioSP()
        {
            InitializeComponent();
            Text = "Soen - Relatório: Serviços Prestados";
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
            btnExportar.Click += (s, e) => RelatorioHelper.ExportarCsv(grid, "servicos_prestados.csv");
            btnImprimir = UIHelpers.CreateButton("Imprimir", new Point(268, 12), new Size(100, 28));
            btnImprimir.Click += (s, e) => RelatorioHelper.Imprimir(grid, "Soen - Serviços Prestados");
            lblTotal = new Label { Text = "", AutoSize = true, Location = new Point(12, 48), Font = new Font("Segoe UI", 9, FontStyle.Bold) };

            grid = new DataGridView
            {
                Location = new Point(12, 72),
                Size = new Size(790, 370),
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            grid.Columns.Add("Data", "Data");
            grid.Columns.Add("Cliente", "Cliente");
            grid.Columns.Add("Servico", "Serviço");
            grid.Columns.Add("Valor", "Valor");
            grid.Columns.Add("Origem", "Origem");

            Controls.AddRange(new Control[] { btnGerar, btnExportar, btnImprimir, lblTotal, grid });
        }

        private void Carregar()
        {
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            grid.Rows.Clear();
            double total = 0;
            foreach (Agendamento a in ServicoDAO.ListarAgendamentos())
            {
                if (a.Status != "concluido") continue;
                grid.Rows.Add(a.DataHora, a.NomeCliente, a.NomeServico, "-", "Agendamento");
            }
            foreach (Orcamento o in ServicoDAO.ListarOrcamentos())
            {
                if (o.Status != "convertido") continue;
                total += o.Valor;
                grid.Rows.Add(o.Data, o.NomeCliente, o.Servico, o.Valor.ToString("N2", cult), "Orçamento");
            }
            lblTotal.Text = "Total de serviços convertidos/concluídos: " + total.ToString("N2", cult);
        }
    }
}