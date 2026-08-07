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
    /// Agendamento de Serviços: escolhe cliente, veículo e serviço, define data/hora,
    /// e acompanha o andamento (agendado → confirmado → concluído / cancelado).
    /// </summary>
    public partial class AgendamentoServicos : BaseForm
    {
        private ComboBox cmbCliente;
        private ComboBox cmbVeiculo;
        private ComboBox cmbServico;
        private DateTimePicker dtpData;
        private TextBox txtHora;
        private TextBox txtObs;
        private Button btnAgendar;
        private Button btnConcluir;
        private Button btnCancelar;
        private Button btnExcluir;
        private Button btnGerarOs;
        private DataGridView grid;
        private StatusStrip statusBar;
        private ToolStripStatusLabel lblStatus;

        public AgendamentoServicos()
        {
            InitializeComponent();
            Text = "Soen - Agendamento de Serviços";
            ClientSize = new Size(860, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;

            CriarInterface();
            CarregarClientes();
            CarregarServicos();
            CarregarAgendamentos();
        }

        private void CriarInterface()
        {
            var l1 = new Label { Text = "Cliente:", AutoSize = true, Location = new Point(12, 20) };
            cmbCliente = new ComboBox { Location = new Point(70, 16), Size = new Size(250, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCliente.SelectedIndexChanged += (s, e) => CarregarVeiculos();

            var l2 = new Label { Text = "Veículo:", AutoSize = true, Location = new Point(335, 20) };
            cmbVeiculo = new ComboBox { Location = new Point(400, 16), Size = new Size(220, 21), DropDownStyle = ComboBoxStyle.DropDownList };

            var l3 = new Label { Text = "Serviço:", AutoSize = true, Location = new Point(635, 20) };
            cmbServico = new ComboBox { Location = new Point(695, 16), Size = new Size(150, 21), DropDownStyle = ComboBoxStyle.DropDownList };

            var l4 = new Label { Text = "Data:", AutoSize = true, Location = new Point(12, 56) };
            dtpData = new DateTimePicker { Location = new Point(70, 53), Size = new Size(120, 20), Format = DateTimePickerFormat.Short };

            var l5 = new Label { Text = "Hora:", AutoSize = true, Location = new Point(210, 56) };
            txtHora = new TextBox { Location = new Point(250, 53), Size = new Size(60, 20), Text = "08:00" };

            var l6 = new Label { Text = "Observações:", AutoSize = true, Location = new Point(340, 56) };
            txtObs = new TextBox { Location = new Point(430, 53), Size = new Size(420, 20) };

            btnAgendar = UIHelpers.CreateButton("Agendar", new Point(12, 90), new Size(110, 28));
            btnAgendar.Click += (s, e) => SalvarAgendamento("agendado");

            btnConcluir = UIHelpers.CreateButton("Concluir Sel.", new Point(130, 90), new Size(110, 28));
            btnConcluir.Click += (s, e) => AlterarStatus("concluido");

            btnCancelar = UIHelpers.CreateButton("Cancelar Sel.", new Point(248, 90), new Size(110, 28));
            btnCancelar.Click += (s, e) => AlterarStatus("cancelado");

            btnExcluir = UIHelpers.CreateButton("Excluir Sel.", new Point(366, 90), new Size(110, 28));
            btnExcluir.Click += (s, e) => Excluir();

            btnGerarOs = UIHelpers.CreateButton("Gerar OS do Sel.", new Point(484, 90), new Size(120, 28));
            btnGerarOs.Click += (s, e) => GerarOsDoAgendamento();

            grid = new DataGridView
            {
                Location = new Point(12, 132),
                Size = new Size(830, 350),
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

            Controls.AddRange(new Control[] { l1, cmbCliente, l2, cmbVeiculo, l3, cmbServico,
                l4, dtpData, l5, txtHora, l6, txtObs,
                btnAgendar, btnConcluir, btnCancelar, btnExcluir, btnGerarOs, grid });
            // BaseForm já adicionou o StatusStrip ao Controls no construtor.
        }

        private void CarregarClientes()
        {
            cmbCliente.Items.Clear();
            foreach (Cliente c in ClienteDAO.Listar(""))
                cmbCliente.Items.Add(new ComboCliente { Id = c.Id, Nome = c.NomeRazao });
            if (cmbCliente.Items.Count > 0) cmbCliente.SelectedIndex = 0;
        }

        private void CarregarVeiculos()
        {
            cmbVeiculo.Items.Clear();
            var dono = cmbCliente.SelectedItem as ComboCliente;
            if (dono == null) return;
            foreach (Veiculo v in VeiculoDAO.Listar(dono.Id))
                cmbVeiculo.Items.Add(new ComboVeiculo { Veiculo = v });
            if (cmbVeiculo.Items.Count > 0) cmbVeiculo.SelectedIndex = 0;
        }

        private void CarregarServicos()
        {
            cmbServico.Items.Clear();
            foreach (Servico s in ServicoDAO.ListarServicos())
                cmbServico.Items.Add(new ComboServico { Id = s.Id, Nome = s.Nome });
            if (cmbServico.Items.Count > 0) cmbServico.SelectedIndex = 0;
        }

        private void SalvarAgendamento(string status)
        {
            var dono = cmbCliente.SelectedItem as ComboCliente;
            if (dono == null) { MessageBox.Show("Selecione o cliente.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            try
            {
                var a = new Agendamento
                {
                    ClienteId = dono.Id,
                    VeiculoId = (cmbVeiculo.SelectedItem as ComboVeiculo)?.Veiculo.Id,
                    ServicoId = (cmbServico.SelectedItem as ComboServico)?.Id,
                    DataHora = dtpData.Value.ToString("yyyy-MM-dd") + " " + txtHora.Text.Trim(),
                    Status = status,
                    Observacoes = txtObs.Text.Trim()
                };
                ServicoDAO.SalvarAgendamento(a);
                CarregarAgendamentos();
                txtObs.Clear();
                MessageBox.Show("Agendamento registrado!", "SOEN", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CarregarAgendamentos()
        {
            grid.Rows.Clear();
            List<Agendamento> lista = ServicoDAO.ListarAgendamentos();
            foreach (var a in lista)
            {
                grid.Rows.Add(a.Id, a.DataHora, a.NomeCliente, a.VeiculoPlaca, a.NomeServico, a.Status);
            }
            lblStatus.Text = lista.Count + " agendamento(s).";
        }

        private void AlterarStatus(string status)
        {
            var id = AgendamentoSelecionado();
            if (!id.HasValue) return;
            List<Agendamento> lista = ServicoDAO.ListarAgendamentos();
            foreach (var a in lista)
            {
                if (a.Id == id.Value)
                {
                    a.Status = status;
                    ServicoDAO.SalvarAgendamento(a);
                    CarregarAgendamentos();
                    return;
                }
            }
        }

        private long? AgendamentoSelecionado()
        {
            if (grid.SelectedRows.Count == 0) return null;
            return Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
        }

        private void Excluir()
        {
            var id = AgendamentoSelecionado();
            if (!id.HasValue) return;
            if (MessageBox.Show("Excluir este agendamento?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ServicoDAO.ExcluirAgendamento(id.Value);
                CarregarAgendamentos();
            }
        }

        private void GerarOsDoAgendamento()
        {
            var id = AgendamentoSelecionado();
            if (!id.HasValue) return;
            Agendamento a = null;
            foreach (var x in ServicoDAO.ListarAgendamentos())
                if (x.Id == id.Value) { a = x; break; }
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

            var form = new CriacaoOrcamentos();
            form.PreencherParaAgendamento(a.ClienteId, a.VeiculoId, a.NomeServico, valor);
            form.Show();
        }
    }

    }
