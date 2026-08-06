using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    public partial class CadastroVeiculo : BaseForm
    {
        private ComboBox cmbClientes; // dono do veículo (seletor adicionado em tempo de execução)

        public CadastroVeiculo()
        {
            InitializeComponent();
            CriarSeletorCliente();
            this.button1.Click += new EventHandler(this.button1_Click);
            textBox1.TextChanged += (s, e) => Validacoes.AplicarMascaraPlaca(textBox1);
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
                    Observacoes = textBox5.Text.Trim()                      // Observações
                };

                VeiculoDAO.Salvar(veiculo);

                MessageBox.Show("Veículo salvo com sucesso!", "SOEN",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                LimparCampos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar: " + ex.Message, "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LimparCampos()
        {
            textBox1.Clear();
            textBox4.Clear();
            textBox5.Clear();
            comboBox1.SelectedIndex = -1;
            comboBox2.SelectedIndex = -1;
            comboBox2.Text = "";
            cmbClientes.SelectedIndex = -1;
            textBox1.Focus();
        }
    }

    }
