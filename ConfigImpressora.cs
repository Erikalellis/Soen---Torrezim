using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using Soen___Torrezim.Data;

namespace Soen___Torrezim
{
    /// <summary>Configuração da impressora padrão do sistema.</summary>
    public partial class ConfigImpressora : Form
    {
        private ComboBox cmbImpressoras;
        private Label lblAtual;
        private Button btnSalvar;
        private Button btnTestar;

        public ConfigImpressora()
        {
            InitializeComponent();
            Text = "Soen - Configuração de Impressora";
            ClientSize = new Size(520, 240);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
        }

        private void CriarInterface()
        {
            var l1 = new Label { Text = "Impressoras instaladas:", AutoSize = true, Location = new Point(12, 20) };
            cmbImpressoras = new ComboBox
            {
                Location = new Point(12, 42),
                Width = 470,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            lblAtual = new Label { Text = "", AutoSize = true, Location = new Point(12, 74) };

            btnSalvar = UIHelpers.CreateButton("Definir como Padrão", new Point(12, 120), new Size(160, 28));
            btnSalvar.Click += (s, e) => Salvar();
            btnTestar = UIHelpers.CreateButton("Imprimir Página de Teste", new Point(190, 120), new Size(180, 28));
            btnTestar.Click += (s, e) => Testar();

            Controls.AddRange(new Control[] { l1, cmbImpressoras, lblAtual, btnSalvar, btnTestar });
        }

        private void Carregar()
        {
            cmbImpressoras.Items.Clear();
            foreach (string p in PrinterSettings.InstalledPrinters)
                cmbImpressoras.Items.Add(p);

            string atual = ImpressoraConfig.Padrao;
            lblAtual.Text = "Impressora padrão atual: " + (string.IsNullOrWhiteSpace(atual) ? "não definida (usar a do diálogo)" : atual);

            if (cmbImpressoras.Items.Count > 0)
            {
                cmbImpressoras.SelectedIndex = 0;
                for (int i = 0; i < cmbImpressoras.Items.Count; i++)
                    if (string.Equals(cmbImpressoras.Items[i].ToString(), atual, StringComparison.OrdinalIgnoreCase))
                    {
                        cmbImpressoras.SelectedIndex = i;
                        break;
                    }
            }
        }

        private void Salvar()
        {
            if (cmbImpressoras.SelectedItem == null)
            {
                MessageBox.Show("Nenhuma impressora selecionada.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ImpressoraConfig.SalvarPadrao(cmbImpressoras.SelectedItem.ToString());
            lblAtual.Text = "Impressora padrão atual: " + cmbImpressoras.SelectedItem;
            MessageBox.Show("Impressora padrão definida com sucesso.", "SOEN", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Testar()
        {
            if (cmbImpressoras.SelectedItem == null) return;
            using (var doc = new PrintDocument())
            {
                doc.PrinterSettings.PrinterName = cmbImpressoras.SelectedItem.ToString();
                doc.DocumentName = "Teste de impressão SOEN";
                doc.PrintPage += (s, e) =>
                {
                    e.Graphics.DrawString("Teste de impressão do sistema SOEN\nImpressora: " + doc.PrinterSettings.PrinterName,
                        new Font("Segoe UI", 12f), Brushes.Black, 40, 40);
                };
                try
                {
                    doc.Print();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Falha ao imprimir: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}