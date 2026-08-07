using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>
    /// Análise de margem: compara custo x preço de venda dos serviços do
    /// catálogo e das peças/produtos do estoque.
    /// </summary>
    public partial class RelatorioMargem : Form
    {
        private ComboBox cmbFiltro;
        private DataGridView grid;
        private Label lblTotal;
        private Button btnGerar;
        private Button btnExportar;
        private Button btnImprimir;

        public RelatorioMargem()
        {
            InitializeComponent();
            Text = "Soen - Relatório: Margem por Serviço/Peça";
            ClientSize = new Size(860, 490);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
        }

        private void CriarInterface()
        {
            var lFiltro = new Label { Text = "Filtrar:", AutoSize = true, Location = new Point(12, 17) };
            cmbFiltro = new ComboBox { Location = new Point(70, 14), Size = new Size(160, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFiltro.Items.Add("Tudo");
            cmbFiltro.Items.Add("Serviços");
            cmbFiltro.Items.Add("Peças");
            cmbFiltro.SelectedIndex = 0;
            cmbFiltro.SelectedIndexChanged += (s, e) => Carregar();

            btnGerar = UIHelpers.CreateButton("Gerar Relatório", new Point(250, 12), new Size(130, 28));
            btnGerar.Click += (s, e) => Carregar();
            btnExportar = UIHelpers.CreateButton("Exportar CSV", new Point(388, 12), new Size(110, 28));
            btnExportar.Click += (s, e) => RelatorioHelper.ExportarCsv(grid, "margem_servicos.csv");
            btnImprimir = UIHelpers.CreateButton("Imprimir", new Point(506, 12), new Size(100, 28));
            btnImprimir.Click += (s, e) => RelatorioHelper.Imprimir(grid, "Soen - Margem por Serviço/Peça");
            lblTotal = new Label { Text = "", AutoSize = true, Location = new Point(12, 50), Font = new Font("Segoe UI", 9, FontStyle.Bold) };

            grid = new DataGridView
            {
                Location = new Point(12, 76),
                Size = new Size(830, 380),
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            grid.Columns.Add("Tipo", "Tipo");
            grid.Columns.Add("Nome", "Nome");
            grid.Columns.Add("Custo", "Custo");
            grid.Columns.Add("Venda", "Venda");
            grid.Columns.Add("MargemR", "Margem R$");
            grid.Columns.Add("MargemP", "Margem %");

            Controls.AddRange(new Control[] { lFiltro, cmbFiltro, btnGerar, btnExportar, btnImprimir, lblTotal, grid });
        }

        private void Carregar()
        {
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            grid.Rows.Clear();

            bool todos = cmbFiltro.SelectedIndex == 0 || cmbFiltro.SelectedIndex == -1;
            bool servicos = todos || cmbFiltro.SelectedIndex == 1;
            bool pecas = todos || cmbFiltro.SelectedIndex == 2;

            double somaMargem = 0;

            if (servicos)
            {
                foreach (Servico s in ServicoDAO.ListarServicos())
                {
                    // Serviço do catálogo: custo não informado; margem sobre o preço de venda.
                    double margemP = s.Preco > 0 ? 100 : 0;
                    grid.Rows.Add("Serviço", s.Nome, "-", s.Preco.ToString("N2", cult),
                        s.Preco.ToString("N2", cult), margemP.ToString("0.##", cult) + "%");
                    somaMargem += s.Preco;
                }
            }

            if (pecas)
            {
                foreach (Produto p in ProdutoDAO.Listar())
                {
                    double margemR = p.Preco - p.Custo;
                    double margemP = p.Custo > 0 ? (margemR / p.Custo) * 100 : 0;
                    grid.Rows.Add("Peça", p.Nome + (string.IsNullOrWhiteSpace(p.Codigo) ? "" : " (" + p.Codigo + ")"),
                        p.Custo.ToString("N2", cult), p.Preco.ToString("N2", cult),
                        margemR.ToString("N2", cult), margemP.ToString("0.##", cult) + "%");
                    somaMargem += margemR;
                }
            }

            lblTotal.Text = "Margem bruta total: R$ " + somaMargem.ToString("N2", cult);
        }
    }
}