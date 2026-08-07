using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Soen___Torrezim.Data;

namespace Soen___Torrezim
{
    /// <summary>
    /// Relatório do tempo médio de atendimento por técnico, calculado entre a
    /// criação e a conversão das OS (Notas de Serviço) já finalizadas.
    /// </summary>
    public partial class RelatorioTempoTecnico : Form
    {
        private DataGridView grid;
        private Label lblTotal;
        private Button btnExportar;
        private Button btnImprimir;

        public RelatorioTempoTecnico()
        {
            InitializeComponent();
            Text = "Soen - Tempo Médio de Atendimento por Técnico";
            ClientSize = new Size(720, 400);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
        }

        private void CriarInterface()
        {
            lblTotal = new Label { Text = "", AutoSize = true, Location = new Point(12, 14), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnExportar = UIHelpers.CreateButton("Exportar CSV", new Point(400, 10), new Size(110, 28));
            btnExportar.Click += (s, e) => RelatorioHelper.ExportarCsv(grid, "tempo_atendimento_tecnicos.csv");
            btnImprimir = UIHelpers.CreateButton("Imprimir", new Point(518, 10), new Size(100, 28));
            btnImprimir.Click += (s, e) => RelatorioHelper.Imprimir(grid, "Soen - Tempo Médio de Atendimento por Técnico");

            grid = new DataGridView
            {
                Location = new Point(12, 48),
                Size = new Size(688, 320),
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            grid.Columns.Add("Tecnico", "Técnico");
            grid.Columns.Add("Qtd", "OS concluídas");
            grid.Columns.Add("Media", "Tempo médio");

            Controls.AddRange(new Control[] { lblTotal, btnExportar, btnImprimir, grid });
        }

        private void Carregar()
        {
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            grid.Rows.Clear();
            double somaHoras = 0;
            long totalOs = 0;
            int tecnicos = 0;
            foreach (var t in ServicoDAO.ListarTemposAtendimento())
            {
                grid.Rows.Add(t.NomeTecnico, t.Quantidade.ToString(),
                    t.MediaHoras.ToString("0.##", cult) + " h");
                somaHoras += t.MediaHoras * t.Quantidade;
                totalOs += t.Quantidade;
                if (t.Quantidade > 0) tecnicos++;
            }
            double mediaGeral = tecnicos == 0 ? 0 : somaHoras / totalOs;
            lblTotal.Text = totalOs == 0
                ? "Nenhuma OS concluída com técnico registrado ainda."
                : "OS concluídas: " + totalOs + "  |  Média geral de atendimento: " +
                  mediaGeral.ToString("0.##", cult) + " h  (" + tecnicos + " técnico(s))";
        }
    }
}