using System;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Configuração dos dados da empresa (nome, contato, endereço, etc.).</summary>
    public partial class EmpresaConfig : Form
    {
        private TextBox txtNome;
        private TextBox txtCnpj;
        private TextBox txtTelefone;
        private TextBox txtEndereco;
        private TextBox txtCidade;
        private TextBox txtEmail;
        private TextBox txtSite;
        private TextBox txtObs;
        private ComboBox cEstado;
        private Button btnSalvar;

        public EmpresaConfig()
        {
            InitializeComponent();
            Text = "Soen - Dados da Empresa";
            ClientSize = new Size(540, 430);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
        }

        private void CriarInterface()
        {
            int lx = 12, fx = 120;

            var l1 = new Label { Text = "Nome/Razão:", AutoSize = true, Location = new Point(lx, 20) };
            txtNome = new TextBox { Location = new Point(fx, 17), Size = new Size(390, 20) };

            var l2 = new Label { Text = "CNPJ:", AutoSize = true, Location = new Point(lx, 50) };
            txtCnpj = new TextBox { Location = new Point(fx, 47), Size = new Size(150, 20) };
            var l3 = new Label { Text = "Telefone:", AutoSize = true, Location = new Point(300, 50) };
            txtTelefone = new TextBox { Location = new Point(360, 47), Size = new Size(150, 20) };

            var l4 = new Label { Text = "Endereço:", AutoSize = true, Location = new Point(lx, 80) };
            txtEndereco = new TextBox { Location = new Point(fx, 77), Size = new Size(390, 20) };

            var l5 = new Label { Text = "Cidade:", AutoSize = true, Location = new Point(lx, 110) };
            txtCidade = new TextBox { Location = new Point(fx, 107), Size = new Size(250, 20) };
            var l6 = new Label { Text = "UF:", AutoSize = true, Location = new Point(385, 110) };
            cEstado = new ComboBox { Location = new Point(415, 107), Size = new Size(95, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cEstado.Items.AddRange(new[]
            {
                "AC","AL","AP","AM","BA","CE","DF","ES","GO","MA","MT","MS","MG","PA","PB",
                "PR","PE","PI","RJ","RN","RS","RO","RR","SC","SP","SE","TO"
            });

            var l7 = new Label { Text = "Email:", AutoSize = true, Location = new Point(lx, 140) };
            txtEmail = new TextBox { Location = new Point(fx, 137), Size = new Size(250, 20) };
            var l8 = new Label { Text = "Site:", AutoSize = true, Location = new Point(385, 140) };
            txtSite = new TextBox { Location = new Point(415, 137), Size = new Size(90, 20) };

            var l9 = new Label { Text = "Observações:", AutoSize = true, Location = new Point(lx, 170) };
            txtObs = new TextBox { Location = new Point(fx, 197), Size = new Size(390, 90), Multiline = true, ScrollBars = ScrollBars.Vertical };

            btnSalvar = UIHelpers.CreateButton("Salvar", new Point(fx, 300), new Size(120, 30));
            btnSalvar.Click += (s, e) => Salvar();

            Controls.AddRange(new Control[] { l1, txtNome, l2, txtCnpj, l3, txtTelefone, l4, txtEndereco,
                l5, txtCidade, l6, cEstado, l7, txtEmail, l8, txtSite, l9, txtObs, btnSalvar });

            txtCnpj.TextChanged += (s, e) => Validacoes.AplicarMascaraCpfCnpj(txtCnpj);
            txtTelefone.TextChanged += (s, e) => Validacoes.AplicarMascaraTelefone(txtTelefone);
        }

        private void Carregar()
        {
            Empresa e = EmpresaDAO.Obter();
            txtNome.Text = e.Nome;
            txtCnpj.Text = e.Cnpj;
            txtTelefone.Text = e.Telefone;
            txtEndereco.Text = e.Endereco;
            txtCidade.Text = e.Cidade;
            txtEmail.Text = e.Email;
            txtSite.Text = e.Site;
            txtObs.Text = e.Observacoes;
            if (!string.IsNullOrWhiteSpace(e.Estado))
            {
                for (int i = 0; i < cEstado.Items.Count; i++)
                    if (cEstado.Items[i].ToString() == e.Estado) { cEstado.SelectedIndex = i; break; }
            }
        }

        private void Salvar()
        {
            var e = new Empresa();
            e.Nome = txtNome.Text.Trim();
            e.Cnpj = txtCnpj.Text.Trim();
            e.Telefone = txtTelefone.Text.Trim();
            e.Endereco = txtEndereco.Text.Trim();
            e.Cidade = txtCidade.Text.Trim();
            e.Estado = cEstado.SelectedItem?.ToString() ?? "";
            e.Email = txtEmail.Text.Trim();
            e.Site = txtSite.Text.Trim();
            e.Observacoes = txtObs.Text.Trim();
            EmpresaDAO.Salvar(e);
            MessageBox.Show("Dados da empresa salvos!", "SOEN", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}