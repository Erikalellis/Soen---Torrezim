using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Confirmação de Serviços: confirma os agendamentos aguardando confirmação.</summary>
    public partial class ConfirmacaoS : Form
    {
        private Button btnConfirmar;
        private Button btnAtualizar;
        private DataGridView grid;
        private StatusStrip statusBar;
        private ToolStripStatusLabel lblStatus;

        public ConfirmacaoS()
        {
            InitializeComponent();
            Text = "Soen - Confirmação de Serviços";
            ClientSize = new Size(820, 460);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
        }

        private void CriarInterface()
        {
            btnConfirmar = UIHelpers.CreateButton("Confirmar Sel.", new Point(12, 15), new Size(120, 28));
            btnConfirmar.Click += (s, e) => Confirmar();
            btnAtualizar = UIHelpers.CreateButton("Atualizar", new Point(142, 15), new Size(100, 28));
            btnAtualizar.Click += (s, e) => Carregar();

            grid = new DataGridView
            {
                Location = new Point(12, 55),
                Size = new Size(790, 380),
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
            statusBar.Location = new Point(0, 478);

            Controls.AddRange(new Control[] { btnConfirmar, btnAtualizar, grid, statusBar });
        }

        private void Carregar()
        {
            grid.Rows.Clear();
            int pendentes = 0;
            foreach (Agendamento a in ServicoDAO.ListarAgendamentos())
            {
                if (a.Status == "agendado")
                {
                    pendentes++;
                    grid.Rows.Add(a.Id, a.DataHora, a.NomeCliente, a.VeiculoPlaca, a.NomeServico, a.Status);
                }
            }
            lblStatus.Text = pendentes + " agendamento(s) aguardando confirmação.";
        }

        private void Confirmar()
        {
            if (grid.SelectedRows.Count == 0) return;
            long id = Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
            foreach (Agendamento a in ServicoDAO.ListarAgendamentos())
            {
                if (a.Id == id)
                {
                    a.Status = "confirmado";
                    ServicoDAO.SalvarAgendamento(a);
                    break;
                }
            }
            Carregar();
        }
    }
}