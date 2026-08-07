using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Gerenciamento de Horários: reorganiza a data/hora dos agendamentos.</summary>
    public partial class GerenciamentoH : Form
    {
        private DataGridView grid;
        private DateTimePicker dtpData;
        private TextBox txtHora;
        private Button btnAplicar;
        private Button btnAtualizar;
        private StatusStrip statusBar;
        private ToolStripStatusLabel lblStatus;

        public GerenciamentoH()
        {
            InitializeComponent();
            Text = "Soen - Gerenciamento de Horários";
            ClientSize = new Size(820, 470);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
        }

        private void CriarInterface()
        {
            var l1 = new Label { Text = "Nova data:", AutoSize = true, Location = new Point(12, 20) };
            dtpData = new DateTimePicker { Location = new Point(90, 17), Size = new Size(110, 20), Format = DateTimePickerFormat.Short };
            var l2 = new Label { Text = "Nova hora:", AutoSize = true, Location = new Point(215, 20) };
            txtHora = new TextBox { Location = new Point(285, 17), Size = new Size(55, 20), Text = "08:00" };
            btnAplicar = UIHelpers.CreateButton("Aplicar em Sel.", new Point(350, 15), new Size(110, 28));
            btnAplicar.Click += (s, e) => Aplicar();
            btnAtualizar = UIHelpers.CreateButton("Atualizar", new Point(470, 15), new Size(100, 28));
            btnAtualizar.Click += (s, e) => Carregar();

            grid = new DataGridView
            {
                Location = new Point(12, 55),
                Size = new Size(790, 390),
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            grid.Columns.Add("Id", "Nº");
            grid.Columns.Add("DataHora", "Data/Hora");
            grid.Columns.Add("Cliente", "Cliente");
            grid.Columns.Add("Veiculo", "Veículo");
            grid.Columns.Add("Servico", "Serviço");
            grid.Columns.Add("Status", "Status");
            grid.Columns["Cliente"].FillWeight = 2f;
            grid.Columns["Servico"].FillWeight = 2f;

            statusBar = new StatusStrip();
            lblStatus = new ToolStripStatusLabel(" ");
            statusBar.Items.Add(lblStatus);
            statusBar.Location = new Point(0, 470);

            Controls.AddRange(new Control[] { l1, dtpData, l2, txtHora, btnAplicar, btnAtualizar, grid, statusBar });
        }

        private void Carregar()
        {
            grid.Rows.Clear();
            var lista = ServicoDAO.ListarAgendamentos();
            foreach (var a in lista)
                grid.Rows.Add(a.Id, a.DataHora, a.NomeCliente, a.VeiculoPlaca, a.NomeServico, a.Status);
            lblStatus.Text = lista.Count + " agendamento(s).";
        }

        private void Aplicar()
        {
            if (grid.SelectedRows.Count == 0) return;
            long id = Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
            string novo = dtpData.Value.ToString("yyyy-MM-dd") + " " + txtHora.Text.Trim();
            foreach (Agendamento a in ServicoDAO.ListarAgendamentos())
            {
                if (a.Id == id)
                {
                    a.DataHora = novo;
                    ServicoDAO.SalvarAgendamento(a);
                    break;
                }
            }
            Carregar();
        }
    }
}