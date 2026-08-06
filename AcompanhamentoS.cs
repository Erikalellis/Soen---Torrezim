using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Acompanhamento de Serviços: acompanha o andamento dos agendamentos.</summary>
    public partial class AcompanhamentoS : Form
    {
        private ComboBox cmbFiltro;
        private Button btnAvancar;
        private Button btnRecuar;
        private Button btnAtualizar;
        private DataGridView grid;
        private StatusStrip statusBar;
        private ToolStripStatusLabel lblStatus;

        public AcompanhamentoS()
        {
            InitializeComponent();
            Text = "Soen - Acompanhamento de Serviços";
            ClientSize = new Size(820, 480);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
        }

        private void CriarInterface()
        {
            var l1 = new Label { Text = "Filtrar status:", AutoSize = true, Location = new Point(12, 20) };
            cmbFiltro = new ComboBox { Location = new Point(100, 16), Size = new Size(180, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFiltro.Items.Add("(todos)");
            cmbFiltro.Items.Add("agendado");
            cmbFiltro.Items.Add("confirmado");
            cmbFiltro.Items.Add("concluido");
            cmbFiltro.Items.Add("cancelado");
            cmbFiltro.SelectedIndex = 0;
            cmbFiltro.SelectedIndexChanged += (s, e) => Carregar();

            btnAvancar = UIHelpers.CreateButton("Avançar Status", new Point(300, 15), new Size(120, 28));
            btnAvancar.Click += (s, e) => AvancarStatus();
            btnRecuar = UIHelpers.CreateButton("Cancelar Sel.", new Point(430, 15), new Size(110, 28));
            btnRecuar.Click += (s, e) => Alterar("cancelado");
            btnAtualizar = UIHelpers.CreateButton("Atualizar", new Point(548, 15), new Size(100, 28));
            btnAtualizar.Click += (s, e) => Carregar();

            grid = new DataGridView
            {
                Location = new Point(12, 55),
                Size = new Size(790, 400),
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

            Controls.AddRange(new Control[] { l1, cmbFiltro, btnAvancar, btnRecuar, btnAtualizar, grid, statusBar });
        }

        private void Carregar()
        {
            grid.Rows.Clear();
            string filtro = cmbFiltro.SelectedItem?.ToString() == "(todos)" ? "" : cmbFiltro.SelectedItem?.ToString();
            List<Agendamento> lista = ServicoDAO.ListarAgendamentos();
            foreach (var a in lista)
            {
                if (!string.IsNullOrEmpty(filtro) && a.Status != filtro) continue;
                grid.Rows.Add(a.Id, a.DataHora, a.NomeCliente, a.VeiculoPlaca, a.NomeServico, a.Status);
            }
            lblStatus.Text = lista.Count + " agendamento(s) - fluxo: agendado → confirmado → concluido";
        }

        private void AvancarStatus()
        {
            var id = Selecionado();
            if (!id.HasValue) return;
            List<Agendamento> lista = ServicoDAO.ListarAgendamentos();
            foreach (var a in lista)
            {
                if (a.Id != id.Value) continue;
                if (a.Status == "agendado") a.Status = "confirmado";
                else if (a.Status == "confirmado") a.Status = "concluido";
                else
                {
                    MessageBox.Show("Não é possível avançar (status atual: " + a.Status + ").", "Atenção",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                ServicoDAO.SalvarAgendamento(a);
                break;
            }
            Carregar();
        }

        private void Alterar(string status)
        {
            var id = Selecionado();
            if (!id.HasValue) return;
            List<Agendamento> lista = ServicoDAO.ListarAgendamentos();
            foreach (var a in lista)
            {
                if (a.Id == id.Value)
                {
                    a.Status = status;
                    ServicoDAO.SalvarAgendamento(a);
                    break;
                }
            }
            Carregar();
        }

        private long? Selecionado()
        {
            if (grid.SelectedRows.Count == 0) return null;
            return Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
        }
    }
}