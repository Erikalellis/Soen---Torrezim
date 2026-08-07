using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    public partial class CadastroVeiculo : BaseForm
    {
        private ComboBox cmbClientes; // dono do veículo (seletor adicionado em tempo de execução)
        private TextBox txtKm;
        private DateTimePicker dtpRevisao;

        public CadastroVeiculo()
        {
            InitializeComponent();
            CriarSeletorCliente();
            CriarCamposRevisao();
            this.button1.Click += new EventHandler(this.button1_Click);
            textBox1.TextChanged += (s, e) => Validacoes.AplicarMascaraPlaca(textBox1);
        }

        // Campo de quilometragem atual e próxima revisão programada.
        private void CriarCamposRevisao()
        {
            var lKm = new Label { Text = "Quilometragem (KM):", AutoSize = true, Location = new Point(190, 82) };
            txtKm = new TextBox { Location = new Point(190, 100), Size = new Size(160, 20) };
            txtKm.TextChanged += (s, e) => Validacoes.AplicarMascaraValor(txtKm);

            var lRev = new Label { Text = "Próxima revisão:", AutoSize = true, Location = new Point(190, 132) };
            dtpRevisao = new DateTimePicker
            {
                Location = new Point(190, 150),
                Size = new Size(160, 20),
                Format = DateTimePickerFormat.Short,
                CheckBox = true,
                Checked = false
            };

            Controls.Add(lKm);
            Controls.Add(txtKm);
            Controls.Add(lRev);
            Controls.Add(dtpRevisao);
        }

        // Adiciona o campo "Cliente (dono)" no formulário.
        private void CriarSeletorCliente()
        {
            var lbl = new Label
            {
                Text = "Cliente (dono):",
                AutoSize = true,
                Location = new Point(16, 44)
            };

            cmbClientes = new ComboBox
            {
                Location = new Point(16, 62),
                Size = new Size(431, 21),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            CarregarClientes();

            Controls.Add(lbl);
            Controls.Add(cmbClientes);
        }

        private void CarregarClientes()
        {
            cmbClientes.Items.Clear();
            cmbClientes.SelectedIndex = -1;

            foreach (Cliente c in ClienteDAO.Listar(""))
            {
                string rotulo = c.NomeRazao + (string.IsNullOrEmpty(c.CpfCnpj) ? "" : " (" + c.CpfCnpj + ")");
                cmbClientes.Items.Add(new ComboCliente { Id = c.Id, Nome = rotulo });
            }
        }

        private void label2_Click(object sender, EventArgs e) { }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e) { }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // ===== Salvar =====
        private void button1_Click(object sender, EventArgs e)
        {
            var dono = cmbClientes.SelectedItem as ComboCliente;
            if (dono == null)
            {
                MessageBox.Show("Selecione o Cliente (dono) do veículo.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string placa = Validacoes.SoDigitos(textBox1.Text);
            if (placa.Length == 0)
            {
                MessageBox.Show("Informe a placa do veículo.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Focus();
                return;
            }

            try
            {
                var veiculo = new Veiculo
                {
                    ClienteId   = dono.Id,
                    Placa       = textBox1.Text.Trim().ToUpper(),          // Placa
                    Marca       = (comboBox1.SelectedItem ?? "").ToString(), // Marca
                    Modelo      = comboBox2.Text.Trim(),                    // Modelo
                    Cor         = textBox4.Text.Trim(),                     // Cor
                    Observacoes = textBox5.Text.Trim(),                     // Observações
                    Quilometragem = LerKm(),
                    ProximaRevisao = dtpRevisao.Checked ? dtpRevisao.Value.ToString("yyyy-MM-dd") : null
                };

                VeiculoDAO.Salvar(veiculo);

                MessageBox.Show("Veículo salvo com sucesso!", "SOEN",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                LimparCampos();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                MessageBox.Show("Erro ao salvar. Veja o log para detalhes.", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LimparCampos()
        {
            textBox1.Clear();
            textBox4.Clear();
            textBox5.Clear();
            txtKm.Clear();
            dtpRevisao.Checked = false;
            comboBox1.SelectedIndex = -1;
            comboBox2.SelectedIndex = -1;
            comboBox2.Text = "";
            cmbClientes.SelectedIndex = -1;
            textBox1.Focus();
        }

        private double LerKm()
        {
            double km = 0;
            double.TryParse(txtKm.Text, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out km);
            return km;
        }
    }

    }
