using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    public partial class CadastroCliente : BaseForm
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
            textBox1.TextChanged += (s, e) => Validacoes.AplicarMascaraCpfCnpj(textBox1);
            textBox13.TextChanged += (s, e) => Validacoes.AplicarMascaraTelefone(textBox13);
            textBox14.TextChanged += (s, e) => Validacoes.AplicarMascaraTelefone(textBox14);
            textBox5.TextChanged += (s, e) => Validacoes.AplicarMascaraCep(textBox5);
        }

        // Abre o cadastro já preenchido com um cliente existente (modo edição).
        public CadastroCliente(Cliente cliente)
            : this()
        {
            Preencher(cliente);
        }

        private void label10_Click(object sender, EventArgs e) { }

        private void label13_Click(object sender, EventArgs e) { }

        private void label1_Click(object sender, EventArgs e) { }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private Cliente _cliente; // não-nulo = modo edição

        private void Preencher(Cliente c)
        {
            _cliente = c;
            textBox1.Text = c.CpfCnpj;      // CPF/CNPJ
            textBox2.Text = c.NomeRazao;      // Nome
            textBox4.Text = c.Sexo;           // Sexo
            textBox5.Text = c.Cep;            // CEP
            textBox6.Text = c.Endereco;       // Endereço
            textBox7.Text = c.Bairro;         // Bairro
            textBox8.Text = c.Nascimento;     // Nascimento
            textBox9.Text = c.Responsavel;    // Responsável
            textBox10.Text = c.Funcao;        // Função
            textBox11.Text = c.Complemento;   // Complemento
            textBox12.Text = c.Cidade;        // Cidade
            textBox13.Text = c.Fone1;         // Telefone 1
            textBox14.Text = c.Fone2;         // Telefone 2
            textBox15.Text = c.Email1;        // Email 1
            textBox16.Text = c.Email2;        // Email 2
            SelecionarEstado(c.Estado);
            Text = "Soen - Edição de Cadastro de Clientes";
            label2.Text = "Edição de Cadastro de Clientes";
        }

        private void SelecionarEstado(string uf)
        {
            if (string.IsNullOrWhiteSpace(uf)) return;
            for (int i = 0; i < comboBox1.Items.Count; i++)
            {
                string nome = comboBox1.Items[i].ToString();
                string sigla;
                if (EstadosUf.TryGetValue(nome, out sigla) && sigla == uf)
                {
                    comboBox1.SelectedIndex = i;
                    return;
                }
            }
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

            string cpfCnpj = Validacoes.SoDigitos(textBox1.Text);
            if (cpfCnpj.Length > 0)
            {
                bool valido = cpfCnpj.Length == 11 ? Validacoes.CpfValido(cpfCnpj)
                                                   : cpfCnpj.Length == 14 && Validacoes.CnpjValido(cpfCnpj);
                if (!valido)
                {
                    MessageBox.Show("CPF/CNPJ inválido. Verifique os números digitados.", "Atenção",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textBox1.Focus();
                    return;
                }
            }

            try
            {
                var cliente = new Cliente
                {
                    Id          = _cliente != null ? _cliente.Id : 0,
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

                MessageBox.Show(_cliente != null
                    ? "Cadastro atualizado com sucesso!"
                    : "Cliente salvo com sucesso!",
                    "SOEN", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LimparCampos();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                MessageBox.Show("Erro ao salvar. Veja o log para detalhes.", "Erro",
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
            _cliente = null;
            Text = "Soen - Cadastro de Clientes";
            label2.Text = "Cadastro de Clientes";
            textBox2.Focus();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}