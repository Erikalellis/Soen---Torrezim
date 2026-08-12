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
    /// Gestão de manutenções: escolhe o veículo, registra uma manutenção
    /// e acompanha o histórico (aberta / concluída / cancelada).
    /// </summary>
    public partial class ManutecaoVeiculo : BaseForm
    {
        private ComboBox cmbVeiculo;
        private DateTimePicker dtpData;
        private TextBox txtDescricao;
        private TextBox txtValor;
        private ComboBox cmbStatus;
        private Button btnSalvar;
        private Button btnConcluir;
        private Button btnExcluir;
        private DataGridView grid;
        private ToolStripStatusLabel lblStatus;

        private long? VeiculoSelecionado
        {
            get { return (cmbVeiculo.SelectedItem as ComboVeiculoItem)?.Id; }
        }

        public ManutecaoVeiculo()
        {
            InitializeComponent();
            Text = "Soen - Manutenções dos Veículos";
            ClientSize = new Size(780, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;

            CriarInterface();
            CarregarVeiculos();
            CarregarManutencoes();
        }

        private void CriarInterface()
        {
            // Seletor de veículo
            var lblVeiculo = new Label { Text = "Veículo:", AutoSize = true, Location = new Point(12, 16) };
            cmbVeiculo = new ComboBox { Location = new Point(70, 13), Size = new Size(330, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbVeiculo.SelectedIndexChanged += (s, e) => CarregarManutencoes();

            // Campo de dados da manutenção
            var lblData = new Label { Text = "Data:", AutoSize = true, Location = new Point(12, 56) };
            dtpData = new DateTimePicker { Location = new Point(70, 53), Size = new Size(130, 20), Format = DateTimePickerFormat.Short };

            var lblDesc = new Label { Text = "Descrição:", AutoSize = true, Location = new Point(12, 84) };
            txtDescricao = new TextBox { Location = new Point(70, 81), Size = new Size(380, 20) };

            var lblValor = new Label { Text = "Valor (R$):", AutoSize = true, Location = new Point(12, 112) };
            txtValor = new TextBox { Location = new Point(70, 109), Size = new Size(100, 20) };

            var lblStatus2 = new Label { Text = "Status:", AutoSize = true, Location = new Point(190, 112) };
            cmbStatus = new ComboBox { Location = new Point(240, 109), Size = new Size(140, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbStatus.Items.AddRange(new object[] { "aberta", "concluida", "cancelada" });
            cmbStatus.SelectedIndex = 0;

            btnSalvar = UIHelpers.CreateButton("Registrar Manutenção", new Point(12, 145), new Size(150, 28));
            btnSalvar.Click += (s, e) => SalvarManutencao();

            btnConcluir = UIHelpers.CreateButton("Concluir Selecionada", new Point(170, 145), new Size(140, 28));
            btnConcluir.Click += (s, e) => AlterarStatus("concluida");

            btnExcluir = UIHelpers.CreateButton("Excluir Selecionada", new Point(318, 145), new Size(132, 28));
            btnExcluir.Click += (s, e) => ExcluirManutencao();

            // Grid do histórico
            grid = new DataGridView
            {
                Location = new Point(12, 185),
                Size = new Size(752, 295),
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
            grid.Columns.Add("Descricao", "Descrição");
            grid.Columns.Add("Valor", "Valor (R$)");
            grid.Columns.Add("Status", "Status");
            grid.Columns["Descricao"].FillWeight = 4f;
            grid.Columns["Data"].FillWeight = 1f;
            grid.Columns["Status"].FillWeight = 1.2f;

            // usa StatusStrip padrão da BaseForm
            lblStatus = BaseStatusLabel;

            Controls.AddRange(new Control[] {
                lblVeiculo, cmbVeiculo, lblData, dtpData, lblDesc, txtDescricao,
                lblValor, txtValor, lblStatus2, cmbStatus,
                btnSalvar, btnConcluir, btnExcluir, grid
            });
            // BaseForm já adicionou o StatusStrip ao Controls no construtor.
        }

        private void CarregarVeiculos()
        {
            cmbVeiculo.Items.Clear();
            cmbVeiculo.SelectedIndex = -1;
            foreach (Veiculo v in VeiculoDAO.Listar(null))
            {
                string rotulo = v.Placa + " - " + v.Marca + " " + v.Modelo;
                if (!string.IsNullOrEmpty(v.NomeCliente)) rotulo += " (" + v.NomeCliente + ")";
                cmbVeiculo.Items.Add(new ComboVeiculoItem { Id = v.Id, Rotulo = rotulo });
            }
            if (cmbVeiculo.Items.Count > 0) cmbVeiculo.SelectedIndex = 0;
        }

        private void CarregarManutencoes()
        {
            grid.Rows.Clear();
            if (!VeiculoSelecionado.HasValue) { lblStatus.Text = ""; return; }

            List<Manutencao> lista = ManutencaoDAO.Listar(VeiculoSelecionado.Value);
            foreach (var m in lista)
            {
                grid.Rows.Add(m.Id, m.Data, m.Descricao, m.Valor.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")), m.Status);
            }
            lblStatus.Text = lista.Count + " manutenção(ões) para o veículo selecionado.";
        }

        private void SalvarManutencao()
        {
            if (!VeiculoSelecionado.HasValue)
            {
                MessageBox.Show("Selecione um veículo.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtDescricao.Text))
            {
                MessageBox.Show("Informe a descrição da manutenção.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double valor;
            if (!double.TryParse(txtValor.Text, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out valor))
            {
                valor = 0;
            }

            try
            {
                var m = new Manutencao
                {
                    VeiculoId  = VeiculoSelecionado.Value,
                    ClienteId  = null,
                    Data       = dtpData.Value.ToString("yyyy-MM-dd"),
                    Descricao  = txtDescricao.Text.Trim(),
                    Valor      = valor,
                    Status     = cmbStatus.SelectedItem?.ToString() ?? "aberta"
                };
                ManutencaoDAO.Salvar(m);

                txtDescricao.Clear();
                txtValor.Clear();
                cmbStatus.SelectedIndex = 0;
                CarregarManutencoes();
                MessageBox.Show("Manutenção registrada!", "SOEN", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                MessageBox.Show("Erro ao salvar. Veja o log para detalhes.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AlterarStatus(string novoStatus)
        {
            var m = ManutencaoSelecionada();
            if (m == null)
            {
                MessageBox.Show("Selecione uma manutenção na lista.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            m.Status = novoStatus;
            ManutencaoDAO.Salvar(m);
            CarregarManutencoes();
        }

        private void ExcluirManutencao()
        {
            if (!Sessao.PrepararExclusao()) return;
            var m = ManutencaoSelecionada();
            if (m == null)
            {
                MessageBox.Show("Selecione uma manutenção na lista.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Excluir esta manutenção?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ManutencaoDAO.Excluir(m.Id);
                CarregarManutencoes();
            }
        }

        private Manutencao ManutencaoSelecionada()
        {
            if (grid.SelectedRows.Count == 0) return null;
            var id = Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
            return ManutencaoDAO.BuscarPorId(id);
        }
    }

    }
