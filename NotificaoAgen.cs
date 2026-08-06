using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Notificações de Agendamentos: lista agendamentos de hoje e futuros (não concluídos/cancelados).</summary>
    public partial class NotificaoAgen : Form
    {
        private ComboBox cmbPeriodo;
        private Button btnAtualizar;
        private DataGridView grid;
        private StatusStrip statusBar;
        private ToolStripStatusLabel lblStatus;

        public NotificaoAgen()
        {
            InitializeComponent();
            Text = "Soen - Notificações de Agendamentos";
            ClientSize = new Size(820, 460);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
        }

        private void CriarInterface()
        {
            var l1 = new Label { Text = "Período:", AutoSize = true, Location = new Point(12, 20) };
            cmbPeriodo = new ComboBox { Location = new Point(70, 16), Size = new Size(160, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPeriodo.Items.Add("Hoje");
            cmbPeriodo.Items.Add("Próximos 7 dias");
            cmbPeriodo.Items.Add("Próximos 30 dias");
            cmbPeriodo.SelectedIndex = 0;
            cmbPeriodo.SelectedIndexChanged += (s, e) => Carregar();

            btnAtualizar = new Button { Text = "Atualizar", Location = new Point(240, 15), Size = new Size(100, 28), BackColor = SystemColors.AppWorkspace };
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

            Controls.AddRange(new Control[] { l1, cmbPeriodo, btnAtualizar, grid, statusBar });
        }

        private void Carregar()
        {
            grid.Rows.Clear();
            int dias = cmbPeriodo.SelectedIndex == 0 ? 0 : (cmbPeriodo.SelectedIndex == 1 ? 7 : 30);
            DateTime hoje = DateTime.Today;
            DateTime limite = hoje.AddDays(dias);
            int cont = 0;
            foreach (Agendamento a in ServicoDAO.ListarAgendamentos())
            {
                if (a.Status == "concluido" || a.Status == "cancelado") continue;
                DateTime dh;
                if (!DateTime.TryParse(a.DataHora, out dh)) continue;
                if (dh.Date < hoje || dh.Date > limite) continue;
                cont++;
                grid.Rows.Add(a.Id, a.DataHora, a.NomeCliente, a.VeiculoPlaca, a.NomeServico, a.Status);
            }
            lblStatus.Text = cont + " agendamento(s) no período.";
        }
    }
}