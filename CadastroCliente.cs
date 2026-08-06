using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    public partial class CadastroCliente : Form
    {
        // Converte o nome completo de um estado (Item do comboBox) para a sigla UF.
        private static readonly Dictionary<string, string> EstadosUf = new Dictionary<string, string>
        {
            { "Acre", "AC" }, { "Alagoas", "AL" }, { "Amapá", "AP" }, { "Amazonas", "AM" },
            { "Bahia", "BA" }, { "Ceará", "CE" }, { "Distrito Federal", "DF" }, { "Espírito Santo", "ES" },
            { "Goiás", "GO" }, { "Maranhão", "MA" }, { "Mato Grosso", "MT" }, { "Mato Grosso do Sul", "MS" },
            { "Minas Gerais", "MG" }, { "Pará", "PA" }, { "Paraíba", "PB" }, { "Paraná", "PR" },
            { "Piauí", "PI" }, { "Rio de Janeiro", "RJ" }, { "Rio Grande do Norte", "RN" },
            { "Rio Grande do Sul", "RS" }, { "Rondônia", "RO" }, { "Roraima", "RR" },
            { "Santa Catarina", "SC" }, { "São Paulo", "SP" }, { "Sergipe", "SE" }, { "Tocantins", "TO" }
        };

        public CadastroCliente()
        {
            InitializeComponent();
            // Conecta o botão "Salvar" ao método que grava no banco.
            this.button1.Click += new EventHandler(this.button1_Click);
        }

        private void label10_Click(object sender, EventArgs e) { }

        private void label13_Click(object sender, EventArgs e) { }

        private void label1_Click(object sender, EventArgs e) { }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // ===== Salvar no banco =====
        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox2.Text))
            {
                MessageBox.Show("Informe o Nome / Razão Social para salvar.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var cliente = new Cliente
                {
                    Tipo        = "cliente",
                    CpfCnpj     = textBox1.Text.Trim(),
                    NomeRazao   = textBox2.Text.Trim(),
                    Sexo        = textBox4.Text.Trim(),
                    Cep         = textBox5.Text.Trim(),
                    Endereco    = textBox6.Text.Trim(),
                    Bairro      = textBox7.Text.Trim(),
                    Nascimento  = textBox8.Text.Trim(),
                    Complemento = textBox11.Text.Trim(),
                    Cidade      = textBox12.Text.Trim(),
                    Estado      = ObterUf(comboBox1.SelectedItem as string),
                    Fone1       = textBox13.Text.Trim(),
                    Fone2       = textBox14.Text.Trim(),
                    Email1      = textBox15.Text.Trim(),
                    Email2      = textBox16.Text.Trim(),
                    Responsavel = textBox9.Text.Trim(),
                    Funcao      = textBox10.Text.Trim()
                };

                ClienteDAO.Salvar(cliente);

                MessageBox.Show("Cliente salvo com sucesso!", "SOEN",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                LimparCampos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar: " + ex.Message, "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string ObterUf(string nomeEstado)
        {
            if (string.IsNullOrWhiteSpace(nomeEstado)) return "";
            string uf;
            return EstadosUf.TryGetValue(nomeEstado.Trim(), out uf) ? uf : nomeEstado.Trim();
        }

        private void LimparCampos()
        {
            textBox1.Clear();  textBox2.Clear();  textBox4.Clear();  textBox5.Clear();
            textBox6.Clear();  textBox7.Clear();  textBox8.Clear();  textBox9.Clear();
            textBox10.Clear(); textBox11.Clear(); textBox12.Clear(); textBox13.Clear();
            textBox14.Clear(); textBox15.Clear(); textBox16.Clear();
            comboBox1.SelectedIndex = -1;
            textBox2.Focus();
        }
    }
}