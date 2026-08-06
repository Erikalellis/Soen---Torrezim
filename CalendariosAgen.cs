using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Calendário de Agendamentos: mostra os agendamentos de cada dia no calendário.</summary>
    public partial class CalendariosAgen : Form
    {
        private MonthCalendar cal;
        private DataGridView grid;
        private Label lblInfo;

        public CalendariosAgen()
        {
            InitializeComponent();
            Text = "Soen - Calendário de Agendamentos";
            ClientSize = new Size(760, 500);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            MostrarDia(DateTime.Today);
        }

        private void CriarInterface()
        {
            cal = new MonthCalendar { Location = new Point(12, 12), MaxSelectionCount = 1 };
            cal.DateChanged += (s, e) => MostrarDia(cal.SelectionStart);

            lblInfo = new Label { Text = "", AutoSize = true, Location = new Point(12, 220), Font = new Font("Segoe UI", 9, FontStyle.Bold) };

            grid = new DataGridView
            {
                Location = new Point(12, 245),
                Size = new Size(720, 220),
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

            Controls.AddRange(new Control[] { cal, lblInfo, grid });
        }

        private void MostrarDia(DateTime dia)
        {
            grid.Rows.Clear();
            string prefixo = dia.ToString("yyyy-MM-dd");
            int cont = 0;
            foreach (Agendamento a in ServicoDAO.ListarAgendamentos())
            {
                if (a.DataHora != null && a.DataHora.StartsWith(prefixo))
                {
                    cont++;
                    grid.Rows.Add(a.Id, a.DataHora, a.NomeCliente, a.VeiculoPlaca, a.NomeServico, a.Status);
                }
            }
            lblInfo.Text = "Agendamentos de " + dia.ToString("dd/MM/yyyy") + ": " + cont;
        }
    }
}