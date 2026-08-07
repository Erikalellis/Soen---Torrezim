using System;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>
    /// Relatório de comissões por Técnico, a partir das OS/Orçamentos que
    /// possuem técnico responsável e comissão calculada.
    /// </summary>
    public partial class RelatorioTecnico : Form
    {
        private ComboBox cmbTecnico;
        private DataGridView grid;
        private Label lblTotal;
        private Button btnGerar;
        private Button btnExportar;
        private Button btnImprimir;

        public RelatorioTecnico()
        {
            InitializeComponent();
            Text = "Soen - Relatório: Comissões por Técnico";
            ClientSize = new Size(860, 490);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            CarregarFiltro();
            Carregar();
        }

        private void CriarInterface()
        {
            var lFiltro = new Label { Text = "Técnico:", AutoSize = true, Location = new Point(12, 17) };
            cmbTecnico = new ComboBox { Location = new Point(90, 14), Size = new Size(220, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTecnico.SelectedIndexChanged += (s, e) => Carregar();

            btnGerar = UIHelpers.CreateButton("Gerar Relatório", new Point(330, 12), new Size(130, 28));
            btnGerar.Click += (s, e) => Carregar();
            btnExportar = UIHelpers.CreateButton("Exportar CSV", new Point(468, 12), new Size(110, 28));
            btnExportar.Click += (s, e) => RelatorioHelper.ExportarCsv(grid, "comissoes_tecnicos.csv");
            btnImprimir = UIHelpers.CreateButton("Imprimir", new Point(586, 12), new Size(100, 28));
            btnImprimir.Click += (s, e) => RelatorioHelper.Imprimir(grid, "Soen - Comissões por Técnico");
            lblTotal = new Label { Text = "", AutoSize = true, Location = new Point(12, 50), Font = new Font("Segoe UI", 9, FontStyle.Bold) };

            grid = new DataGridView
            {
                Location = new Point(12, 76),
                Size = new Size(790, 380),
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            grid.Columns.Add("Data", "Data");
            grid.Columns.Add("Numero", "Nº");
            grid.Columns.Add("Tecnico", "Técnico");
            grid.Columns.Add("Cliente", "Cliente");
            grid.Columns.Add("Valor", "Valor");
            grid.Columns.Add("Comissao", "Comissão");
            grid.Columns.Add("Status", "Status");

            Controls.AddRange(new Control[] { lFiltro, cmbTecnico, btnGerar, btnExportar, btnImprimir, lblTotal, grid });
        }

        private void CarregarFiltro()
        {
            cmbTecnico.Items.Clear();
            cmbTecnico.Items.Add("(todos os técnicos)");
            foreach (Tecnico t in TecnicoDAO.Listar())
                cmbTecnico.Items.Add(new ComboTecnico { Id = t.Id, Nome = t.Nome, Comissao = t.ComissaoPercent });
            cmbTecnico.SelectedIndex = 0;
        }

        private void Carregar()
        {
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            long? filtroTecnico = null;
            if (cmbTecnico != null && cmbTecnico.SelectedIndex > 0)
                filtroTecnico = ((ComboTecnico)cmbTecnico.SelectedItem).Id;

            grid.Rows.Clear();
            double totalComissao = 0;
            foreach (Orcamento o in ServicoDAO.ListarOrcamentos())
            {
                if (filtroTecnico.HasValue && o.TecnicoId != filtroTecnico.Value) continue;
                grid.Rows.Add(o.Data,
                    string.IsNullOrWhiteSpace(o.Numero) ? ("#" + o.Id) : o.Numero,
                    string.IsNullOrWhiteSpace(o.NomeTecnico) ? "—" : o.NomeTecnico,
                    o.NomeCliente,
                    o.Valor.ToString("N2", cult),
                    o.Comissao.ToString("N2", cult),
                    TraduzirStatus(o.Status));
                totalComissao += o.Comissao;
            }
            lblTotal.Text = "Total de comissões: R$ " + totalComissao.ToString("N2", cult);
        }

        private static string TraduzirStatus(string s)
        {
            switch ((s ?? "").Trim().ToLower())
            {
                case "em_aberto": return "Em aberto";
                case "aprovado": return "Aprovado";
                case "recusado": return "Recusado";
                case "convertido": return "Convertido";
                default: return s ?? "";
            }
        }
    }
}