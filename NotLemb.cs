using System;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Lembretes de Tarefas: mostra os agendamentos de hoje como lembretes.</summary>
    public partial class NotLemb : Form
    {
        private DataGridView grid;
        private Label lblInfo;

        public NotLemb()
        {
            InitializeComponent();
            Text = "Soen - Lembretes de Tarefas";
            ClientSize = new Size(720, 420);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
        }

        private void CriarInterface()
        {
            lblInfo = new Label { Text = "", AutoSize = true, Location = new Point(12, 15), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            grid = new DataGridView
            {
                Location = new Point(12, 45),
                Size = new Size(690, 340),
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            grid.Columns.Add("DataHora", "Data/Hora");
            grid.Columns.Add("Cliente", "Cliente");
            grid.Columns.Add("Veiculo", "Veículo");
            grid.Columns.Add("Servico", "Serviço");
            grid.Columns.Add("Status", "Status");
            grid.Columns["Cliente"].FillWeight = 2f;
            grid.Columns["Servico"].FillWeight = 2f;

            Controls.AddRange(new Control[] { lblInfo, grid });
        }

        private void Carregar()
        {
            grid.Rows.Clear();
            string hoje = DateTime.Today.ToString("yyyy-MM-dd");
            int cont = 0;
            foreach (Agendamento a in ServicoDAO.ListarAgendamentos())
            {
                if (a.Status == "concluido" || a.Status == "cancelado") continue;
                if (a.DataHora != null && a.DataHora.StartsWith(hoje))
                {
                    cont++;
                    grid.Rows.Add(a.DataHora, a.NomeCliente, a.VeiculoPlaca, a.NomeServico, a.Status);
                }
            }
            lblInfo.Text = "Lembretes de hoje (" + DateTime.Today.ToString("dd/MM/yyyy") + "): " + cont + " tarefa(s).";
        }
    }
}