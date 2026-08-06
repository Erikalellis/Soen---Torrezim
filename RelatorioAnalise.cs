using System;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Relatório de Análise Gerencial: indicadores globais do negócio.</summary>
    public partial class RelatorioAnalise : Form
    {
        private DataGridView grid;
        private Button btnGerar;
        private Button btnExportar;
        private Button btnImprimir;

        public RelatorioAnalise()
        {
            InitializeComponent();
            Text = "Soen - Análise Gerencial / Desempenho";
            ClientSize = new Size(640, 460);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
        }

        private void CriarInterface()
        {
            btnGerar = new Button { Text = "Gerar Análise", Location = new Point(12, 12), Size = new Size(120, 28), BackColor = SystemColors.AppWorkspace };
            btnGerar.Click += (s, e) => Carregar();
            btnExportar = new Button { Text = "Exportar CSV", Location = new Point(140, 12), Size = new Size(110, 28), BackColor = SystemColors.AppWorkspace };
            btnExportar.Click += (s, e) => RelatorioHelper.ExportarCsv(grid, "analise_gerencial.csv");
            btnImprimir = new Button { Text = "Imprimir", Location = new Point(258, 12), Size = new Size(100, 28), BackColor = SystemColors.AppWorkspace };
            btnImprimir.Click += (s, e) => RelatorioHelper.Imprimir(grid, "Soen - Análise Gerencial");

            grid = new DataGridView
            {
                Location = new Point(12, 50),
                Size = new Size(610, 380),
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            grid.Columns.Add("Indicador", "Indicador");
            grid.Columns.Add("Valor", "Valor");

            Controls.AddRange(new Control[] { btnGerar, btnExportar, btnImprimir, grid });
        }

        private void Carregar()
        {
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            grid.Rows.Clear();

            int clientes = 0, veiculos = 0;
            foreach (Cliente c in ClienteDAO.Listar("")) if (c.Tipo == "cliente") clientes++;
            foreach (Veiculo v in VeiculoDAO.Listar(null)) veiculos++;
            int agendados = 0, concluidos = 0, orcEmAberto = 0, orcAprovados = 0;
            foreach (Agendamento a in ServicoDAO.ListarAgendamentos())
            {
                if (a.Status == "agendado" || a.Status == "confirmado") agendados++;
                if (a.Status == "concluido") concluidos++;
            }
            foreach (Orcamento o in ServicoDAO.ListarOrcamentos())
            {
                if (o.Status == "em_aberto") orcEmAberto++;
                if (o.Status == "aprovado") orcAprovados++;
            }
            double vendas = 0;
            foreach (Venda v in VendaDAO.Listar(null)) vendas += v.ValorTotal;

            grid.Rows.Add("Clientes cadastrados", clientes.ToString());
            grid.Rows.Add("Veículos cadastrados", veiculos.ToString());
            grid.Rows.Add("Serviços agendados (em andamento)", agendados.ToString());
            grid.Rows.Add("Serviços concluídos", concluidos.ToString());
            grid.Rows.Add("Orçamentos em aberto", orcEmAberto.ToString());
            grid.Rows.Add("Orçamentos aprovados", orcAprovados.ToString());
            grid.Rows.Add("Total de vendas", vendas.ToString("N2", cult));
            grid.Rows.Add("Saldo em caixa", CaixaDAO.Saldo().ToString("N2", cult));
        }
    }
}