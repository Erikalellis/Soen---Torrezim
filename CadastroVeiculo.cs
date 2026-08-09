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
        private DateTimePicker dtpGarantia;
        private DateTimePicker dtpIpva;
        private DateTimePicker dtpLicenciamento;
        private Veiculo _veiculo;

        public CadastroVeiculo()
        {
            InitializeComponent();
            CriarSeletorCliente();
            CriarCamposRevisao();
            this.button1.Click += new EventHandler(this.button1_Click);
            textBox1.TextChanged += (s, e) => Validacoes.AplicarMascaraPlaca(textBox1);

            // Atalhos de teclado: F2 salva, F5 limpa, Esc fecha.
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F2) button1.PerformClick();
                else if (e.KeyCode == Keys.F5) LimparCampos();
                else if (e.KeyCode == Keys.Escape) Close();
            };
        }

        /// <summary>Abre em modo edição com os dados do veículo informado.</summary>
        public CadastroVeiculo(Veiculo v) : this()
        {
            if (v == null) return;
            _veiculo = v;
            Text = "Edição de Veículo";
            textBox1.Text = v.Placa;

            var itens = new List<object>();
            foreach (object item in comboBox1.Items) itens.Add(item);
            int idxMarca = itens.IndexOf(v.Marca);
            if (idxMarca >= 0) comboBox1.SelectedIndex = idxMarca;

            comboBox2.Text = v.Modelo;
            textBox4.Text = v.Cor;
            textBox5.Text = v.Observacoes;
            txtKm.Text = v.Quilometragem > 0 ? v.Quilometragem.ToString("0", CultureInfo.GetCultureInfo("pt-BR")) : "";

            if (cmbClientes != null)
            {
                foreach (object item in cmbClientes.Items)
                {
                    var cc = item as ComboCliente;
                    if (cc != null && cc.Id == v.ClienteId) { cmbClientes.SelectedItem = cc; break; }
                }
            }

            DateTimePicker[] datas = { dtpRevisao, dtpGarantia, dtpIpva, dtpLicenciamento };
            string[] venc = { v.ProximaRevisao, v.GarantiaFim, v.IpvaVenc, v.LicenciamentoVenc };
            for (int i = 0; i < datas.Length; i++)
            {
                DateTime d;
                if (!string.IsNullOrWhiteSpace(venc[i]) && DateTime.TryParse(venc[i], out d))
                {
                    datas[i].Checked = true;
                    datas[i].Value = d;
                }
            }
        }

        // Campo de quilometragem atual e próxima revisão programada.
        private void CriarCamposRevisao()
        {
            var lKm = new Label { Text = "Quilometragem (KM):", AutoSize = true, Location = new Point(190, 82) };
            txtKm = new TextBox { Location = new Point(190, 100), Size = new Size(160, 20) };
            txtKm.TextChanged += (s, e) => Validacoes.AplicarMascaraValor(txtKm);

            var lRev = new Label { Text = "Próxima revisão:", AutoSize = true, Location = new Point(190, 132) };
            dtpRevisao = CriarData(190, 150, true);

            var lGar = new Label { Text = "Fim da garantia:", AutoSize = true, Location = new Point(190, 184) };
            dtpGarantia = CriarData(190, 202, true);

            var lIpva = new Label { Text = "Vencimento do IPVA:", AutoSize = true, Location = new Point(190, 236) };
            dtpIpva = CriarData(190, 254, true);

            var lLic = new Label { Text = "Licenciamento vence:", AutoSize = true, Location = new Point(190, 288) };
            dtpLicenciamento = CriarData(190, 306, true);

            Controls.Add(lKm);
            Controls.Add(txtKm);
            Controls.Add(lRev);
            Controls.Add(dtpRevisao);
            Controls.Add(lGar);
            Controls.Add(dtpGarantia);
            Controls.Add(lIpva);
            Controls.Add(dtpIpva);
            Controls.Add(lLic);
            Controls.Add(dtpLicenciamento);
        }

        private DateTimePicker CriarData(int x, int y, bool checkbox)
        {
            var dtp = new DateTimePicker
            {
                Location = new Point(x, y),
                Size = new Size(160, 20),
                Format = DateTimePickerFormat.Short,
                ShowCheckBox = checkbox,
                Checked = false
            };
            return dtp;
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
                    Id          = _veiculo != null ? _veiculo.Id : 0,
                    ClienteId   = dono.Id,
                    Placa       = textBox1.Text.Trim().ToUpper(),          // Placa
                    Marca       = (comboBox1.SelectedItem ?? "").ToString(), // Marca
                    Modelo      = comboBox2.Text.Trim(),                    // Modelo
                    Cor         = textBox4.Text.Trim(),                     // Cor
                    Observacoes = textBox5.Text.Trim(),                     // Observações
                    Quilometragem = LerKm(),
                    ProximaRevisao = dtpRevisao.Checked ? dtpRevisao.Value.ToString("yyyy-MM-dd") : null,
                    GarantiaFim = dtpGarantia.Checked ? dtpGarantia.Value.ToString("yyyy-MM-dd") : null,
                    IpvaVenc = dtpIpva.Checked ? dtpIpva.Value.ToString("yyyy-MM-dd") : null,
                    LicenciamentoVenc = dtpLicenciamento.Checked ? dtpLicenciamento.Value.ToString("yyyy-MM-dd") : null
                };

                VeiculoDAO.Salvar(veiculo);

                MessageBox.Show("Veículo salvo com sucesso!", "SOEN",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (_veiculo != null) Close();
                else LimparCampos();
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
            dtpGarantia.Checked = false;
            dtpIpva.Checked = false;
            dtpLicenciamento.Checked = false;
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
