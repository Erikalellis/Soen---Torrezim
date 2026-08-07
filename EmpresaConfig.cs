using System;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>
    /// Configuração dos dados da empresa (nome, contato, endereço) e da aparência
    /// do sistema (imagem de fundo das janelas e logo usado em documentos).
    /// </summary>
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

        private string _fundoPath;
        private string _logoPath;
        private PictureBox pbg;
        private PictureBox pLogo;
        private ComboBox cboModo;
        private Label lblFundoInfo;
        private Label lblLogoInfo;

        public EmpresaConfig()
        {
            InitializeComponent();
            Text = "Soen - Dados da Empresa";
            ClientSize = new Size(540, 700);
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
            txtObs = new TextBox { Location = new Point(fx, 197), Size = new Size(390, 70), Multiline = true, ScrollBars = ScrollBars.Vertical };

            // ===== Aparência: imagem de fundo das janelas =====
            var lFundo = new Label { Text = "Imagem de fundo das janelas", Font = new Font(Font.FontFamily, 9f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 278) };
            var btnEscFundo = UIHelpers.CreateButton("Escolher imagem...", new Point(120, 274), new Size(150, 28));
            btnEscFundo.Click += (s, e) => EscolherFundo();
            var btnRemFundo = UIHelpers.CreateButton("Remover", new Point(278, 274), new Size(100, 28));
            btnRemFundo.Click += (s, e) => RemoverFundo();

            var lModo = new Label { Text = "Modo:", AutoSize = true, Location = new Point(12, 310) };
            cboModo = new ComboBox { Location = new Point(120, 306), Size = new Size(180, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cboModo.Items.Add("Esticar (preencher)");
            cboModo.Items.Add("Centralizar");
            cboModo.Items.Add("Ladrilhar");

            pbg = new PictureBox
            {
                Location = new Point(12, 336),
                Size = new Size(240, 120),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            lblFundoInfo = new Label { Text = "", AutoSize = true, Location = new Point(260, 336), Size = new Size(260, 100) };

            // ===== Aparência: logo =====
            var lLogo = new Label { Text = "Logo (documentos e recibo)", Font = new Font(Font.FontFamily, 9f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 468) };
            var btnEscLogo = UIHelpers.CreateButton("Escolher logo...", new Point(180, 464), new Size(140, 28));
            btnEscLogo.Click += (s, e) => EscolherLogo();
            var btnRemLogo = UIHelpers.CreateButton("Remover", new Point(330, 464), new Size(100, 28));
            btnRemLogo.Click += (s, e) => RemoverLogo();

            pLogo = new PictureBox
            {
                Location = new Point(12, 496),
                Size = new Size(140, 90),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            lblLogoInfo = new Label { Text = "", AutoSize = true, Location = new Point(160, 496), Size = new Size(360, 80) };

            btnSalvar = UIHelpers.CreateButton("Salvar", new Point(120, 620), new Size(120, 32));
            btnSalvar.Click += (s, e) => Salvar();

            Controls.AddRange(new Control[] { l1, txtNome, l2, txtCnpj, l3, txtTelefone, l4, txtEndereco,
                l5, txtCidade, l6, cEstado, l7, txtEmail, l8, txtSite, l9, txtObs,
                lFundo, btnEscFundo, btnRemFundo, lModo, cboModo, pbg, lblFundoInfo,
                lLogo, btnEscLogo, btnRemLogo, pLogo, lblLogoInfo, btnSalvar });

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

            cboModo.SelectedIndex = string.Equals(e.BackgroundMode, "center", StringComparison.OrdinalIgnoreCase) ? 1
                : string.Equals(e.BackgroundMode, "tile", StringComparison.OrdinalIgnoreCase) ? 2 : 0;

            _fundoPath = e.BackgroundImagePath;
            _logoPath = e.LogoPath;
            AtualizarPreviewFundo();
            AtualizarPreviewLogo();
        }

        private void EscolherFundo()
        {
            using (var dlg = new OpenFileDialog { Filter = "Imagens|*.png;*.jpg;*.jpeg;*.bmp;*.gif", Title = "Escolher imagem de fundo" })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                _fundoPath = dlg.FileName;
                AtualizarPreviewFundo();
            }
        }

        private void RemoverFundo()
        {
            _fundoPath = null;
            AtualizarPreviewFundo();
        }

        private void AtualizarPreviewFundo()
        {
            try
            {
                pbg.Image = string.IsNullOrWhiteSpace(_fundoPath) ? null
                    : (System.IO.File.Exists(_fundoPath) ? Image.FromFile(_fundoPath) : null);
            }
            catch
            {
                pbg.Image = null;
            }
            lblFundoInfo.Text = string.IsNullOrWhiteSpace(_fundoPath)
                ? "Nenhuma imagem definida."
                : "Imagem: " + _fundoPath;
        }

        private void EscolherLogo()
        {
            using (var dlg = new OpenFileDialog { Filter = "Imagens|*.png;*.jpg;*.jpeg;*.bmp;*.gif", Title = "Escolher logo" })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                _logoPath = dlg.FileName;
                AtualizarPreviewLogo();
            }
        }

        private void RemoverLogo()
        {
            _logoPath = null;
            AtualizarPreviewLogo();
        }

        private void AtualizarPreviewLogo()
        {
            try
            {
                pLogo.Image = string.IsNullOrWhiteSpace(_logoPath) ? null
                    : (System.IO.File.Exists(_logoPath) ? Image.FromFile(_logoPath) : null);
            }
            catch
            {
                pLogo.Image = null;
            }
            lblLogoInfo.Text = string.IsNullOrWhiteSpace(_logoPath)
                ? "Nenhum logo definido."
                : "Logo: " + _logoPath;
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

            e.BackgroundImagePath = _fundoPath;
            e.BackgroundMode = cboModo.SelectedIndex == 1 ? "center" : cboModo.SelectedIndex == 2 ? "tile" : "stretch";
            e.LogoPath = _logoPath;
            e.LogoWidth = pLogo.Image == null ? 0 : pLogo.Image.Width;
            e.LogoHeight = pLogo.Image == null ? 0 : pLogo.Image.Height;

            EmpresaDAO.Salvar(e);
            MessageBox.Show("Dados da empresa salvos!\r\nA imagem de fundo é aplicada ao reabrir as janelas.", "SOEN", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}