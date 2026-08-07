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
    /// Histórico de manutenções de todos os veículos (visão geral, somente leitura).
    /// </summary>
    public partial class HistoricoManutencao : Form
    {
        private TextBox txtBusca;
        private Button btnBuscar;
        private Button btnTodos;
        private DataGridView grid;
        private StatusStrip statusBar;
        private ToolStripStatusLabel lblStatus;

        public HistoricoManutencao()
        {
            InitializeComponent();
            Text = "Soen - Histórico de Manutenções";
            ClientSize = new Size(820, 470);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;

            CriarInterface();
            Carregar("");
        }

        private void CriarInterface()
        {
            txtBusca = new TextBox { Location = new Point(12, 16), Size = new Size(280, 22) };
            txtBusca.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Carregar(txtBusca.Text.Trim()); };

            btnBuscar = UIHelpers.CreateButton("Buscar", new Point(300, 12), new Size(90, 26));
            btnBuscar.Click += (s, e) => Carregar(txtBusca.Text.Trim());

            btnTodos = UIHelpers.CreateButton("Todos", new Point(398, 12), new Size(90, 26));
            btnTodos.Click += (s, e) => { txtBusca.Clear(); Carregar(""); };

            grid = new DataGridView
            {
                Location = new Point(12, 52),
                Size = new Size(794, 380),
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            grid.Columns.Add("Id", "Código");
            grid.Columns.Add("Data", "Data");
            grid.Columns.Add("Veiculo", "Veículo (Placa)");
            grid.Columns.Add("Cliente", "Cliente");
            grid.Columns.Add("Descricao", "Descrição");
            grid.Columns.Add("Valor", "Valor (R$)");
            grid.Columns.Add("Status", "Status");
            grid.Columns["Veiculo"].FillWeight = 1.4f;
            grid.Columns["Cliente"].FillWeight = 1.6f;
            grid.Columns["Descricao"].FillWeight = 3f;
            grid.Columns["Valor"].FillWeight = 1.2f;

            statusBar = new StatusStrip();
            lblStatus = new ToolStripStatusLabel(" ");
            statusBar.Items.Add(lblStatus);
            statusBar.Location = new Point(0, 448);

            Controls.AddRange(new Control[] { txtBusca, btnBuscar, btnTodos, grid, statusBar });
        }

        private Button CriarBotao(string texto, int x, int y, EventHandler clique)
        {
            var b = new Button { Text = texto, Location = new Point(x, y), Size = new Size(90, 26), BackColor = SystemColors.AppWorkspace };
            b.Click += clique;
            return b;
        }

        private void Carregar(string filtro)
        {
            grid.Rows.Clear();
            var lista = new List<Manutencao>();

            foreach (var m in ManutencaoDAO.Listar(null))
            {
                bool ok = true;
                if (!string.IsNullOrWhiteSpace(filtro))
                {
                    var f = filtro.ToLower();
                    ok = m.Placa.ToLower().Contains(f)
                         || (m.VeiculoDesc != null && m.VeiculoDesc.ToLower().Contains(f))
                         || (m.NomeCliente != null && m.NomeCliente.ToLower().Contains(f))
                         || (m.Descricao != null && m.Descricao.ToLower().Contains(f));
                }
                if (ok) lista.Add(m);
            }

            foreach (var m in lista)
            {
                string veiculo = m.Placa + " " + (string.IsNullOrEmpty(m.VeiculoDesc) ? "" : m.VeiculoDesc);
                grid.Rows.Add(m.Id, m.Data, veiculo.Trim(), m.NomeCliente, m.Descricao,
                    m.Valor.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")), m.Status);
            }
            lblStatus.Text = lista.Count + " manutenção(ões) encontrada(s).";
        }
    }
}