using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Gerenciamento de Horários: reorganiza a data/hora dos agendamentos.</summary>
    public partial class GerenciamentoH : BaseForm
    {
        private DataGridView grid;
        private DateTimePicker dtpData;
        private TextBox txtHora;
        private Button btnAplicar;
        private Button btnAtualizar;
        private Button btnGerarOs;
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
            btnGerarOs = UIHelpers.CreateButton("Gerar OS do Sel.", new Point(580, 15), new Size(130, 28));
            btnGerarOs.Click += (s, e) => GerarOsDoAgendamento();

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

            // usa StatusStrip padrão da BaseForm
            lblStatus = BaseStatusLabel;

            Controls.AddRange(new Control[] { l1, dtpData, l2, txtHora, btnAplicar, btnAtualizar, btnGerarOs, grid });
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

            DateTime hora;
            if (!DateTime.TryParse(txtHora.Text.Trim(), out hora))
            {
                MessageBox.Show("Informe uma hora válida (ex.: 08:00).", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string novo = dtpData.Value.ToString("yyyy-MM-dd") + " " + hora.ToString("HH:mm");

            if (TemConflito(id, novo))
            {
                MessageBox.Show("Já existe outro agendamento nesta data/hora.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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

        private bool TemConflito(long ignorarId, string dataHora)
        {
            foreach (Agendamento a in ServicoDAO.ListarAgendamentos())
                if (a.Id != ignorarId && a.DataHora == dataHora && a.Status != "cancelado")
                    return true;
            return false;
        }

        private void GerarOsDoAgendamento()
        {
            if (grid.SelectedRows.Count == 0) return;
            long id = Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
            Agendamento a = null;
            foreach (var x in ServicoDAO.ListarAgendamentos())
                if (x.Id == id) { a = x; break; }
            if (a == null) return;
            if (a.Status != "concluido")
            {
                MessageBox.Show("Para gerar uma OS, o agendamento deve estar CONCLUÍDO.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double valor = 0;
            if (a.ServicoId.HasValue)
                foreach (Servico s in ServicoDAO.ListarServicos())
                    if (s.Id == a.ServicoId.Value) { valor = s.Preco; break; }

            Janelas.Abrir(() => new CriacaoOrcamentos()).PreencherParaAgendamento(a.ClienteId, a.VeiculoId, a.NomeServico, valor);
        }
    }
}