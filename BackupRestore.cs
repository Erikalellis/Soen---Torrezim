using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Soen___Torrezim.Data;

namespace Soen___Torrezim
{
    /// <summary>Backup e restauração do banco de dados (arquivo SQLite) + backup automático.</summary>
    public partial class BackupRestore : Form
    {
        private Label lblInfo;
        private Label lblDestino;
        private Label lblUltimo;
        private Button btnBackup;
        private Button btnRestore;
        private CheckBox chkAuto;
        private ComboBox cmbFreq;
        private TextBox txtDestino;
        private TextBox txtReter;
        private Button btnEscolher;
        private Button btnSalvar;
        private Button btnAgora;

        public BackupRestore()
        {
            InitializeComponent();
            Text = "Soen - Backup e Restauração";
            ClientSize = new Size(520, 380);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            CarregarConfig();
        }

        private void CriarInterface()
        {
            var l1 = new Label { Text = "Arquivo do banco:", AutoSize = true, Location = new Point(12, 20) };
            lblInfo = new Label { Text = Database.CaminhoBanco, Location = new Point(12, 40), Size = new Size(490, 40) };
            lblDestino = new Label { Text = "", Location = new Point(12, 305), Size = new Size(490, 60) };

            btnBackup = UIHelpers.CreateButton("Fazer Backup...", new Point(12, 170), new Size(130, 28));
            btnBackup.Click += (s, e) => FazerBackup();

            btnRestore = UIHelpers.CreateButton("Restaurar Backup...", new Point(160, 170), new Size(140, 28));
            btnRestore.Click += (s, e) => Restaurar();

            // Seção: Backup automático
            var l2 = new Label { Text = "Backup automático", Font = new Font(Font.FontFamily, 10f, FontStyle.Bold), Location = new Point(12, 205), AutoSize = true };
            chkAuto = new CheckBox { Text = "Habilitar backup automático", Location = new Point(12, 230), AutoSize = true };

            var l3 = new Label { Text = "Frequência:", Location = new Point(12, 258), AutoSize = true };
            cmbFreq = new ComboBox { Location = new Point(110, 254), Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFreq.Items.Add("Diário");
            cmbFreq.Items.Add("Semanal");
            cmbFreq.Items.Add("Ao sair do sistema");

            lblUltimo = new Label { Text = "Último backup: —", Location = new Point(260, 258), AutoSize = true };

            var l4 = new Label { Text = "Pasta destino:", Location = new Point(12, 288), AutoSize = true };
            txtDestino = new TextBox { Location = new Point(110, 284), Width = 270 };
            btnEscolher = UIHelpers.CreateButton("Escolher...", new Point(388, 284), new Size(100, 26));
            btnEscolher.Click += (s, e) => EscolherPasta();

            var l5 = new Label { Text = "Manter cópias:", Location = new Point(12, 318), AutoSize = true };
            txtReter = new TextBox { Location = new Point(110, 314), Width = 50, Text = "10" };
            btnSalvar = UIHelpers.CreateButton("Salvar Config.", new Point(180, 314), new Size(120, 26));
            btnSalvar.Click += (s, e) => SalvarConfig();

            btnAgora = UIHelpers.CreateButton("Fazer Backup Agora", new Point(320, 314), new Size(140, 26));
            btnAgora.Click += (s, e) => BackupAgora();

            Controls.AddRange(new Control[] { l1, lblInfo, lblDestino, btnBackup, btnRestore,
                l2, chkAuto, l3, cmbFreq, lblUltimo, l4, txtDestino, btnEscolher,
                l5, txtReter, btnSalvar, btnAgora });
        }

        private void CarregarConfig()
        {
            chkAuto.Checked = BackupAgendado.Automatico;
            cmbFreq.SelectedIndex = BackupAgendado.Frequencia == BackupAgendado.FrequenciaSemanal ? 1
                : BackupAgendado.Frequencia == BackupAgendado.FrequenciaSair ? 2 : 0;
            txtDestino.Text = BackupAgendado.Destino;
            txtReter.Text = BackupAgendado.Reter.ToString();
            lblUltimo.Text = "Último backup: " + (string.IsNullOrWhiteSpace(BackupAgendado.UltimoBackup) ? "—" : BackupAgendado.UltimoBackup);
        }

        private void SalvarConfig()
        {
            ConfigDAO.Salvar(BackupAgendado.ChaveAutomatico, chkAuto.Checked ? "1" : "0");
            ConfigDAO.Salvar(BackupAgendado.ChaveDestino, txtDestino.Text.Trim());
            string freq = cmbFreq.SelectedIndex == 1 ? BackupAgendado.FrequenciaSemanal
                : cmbFreq.SelectedIndex == 2 ? BackupAgendado.FrequenciaSair : BackupAgendado.FrequenciaDiario;
            ConfigDAO.Salvar(BackupAgendado.ChaveFrequencia, freq);
            ConfigDAO.Salvar(BackupAgendado.ChaveReter, txtReter.Text.Trim());
            lblDestino.Text = "Configuração do backup automático salva.";
        }

        private void EscolherPasta()
        {
            using (var dlg = new FolderBrowserDialog { SelectedPath = txtDestino.Text.Trim() })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtDestino.Text = dlg.SelectedPath;
            }
        }

        private void BackupAgora()
        {
            string caminho = BackupAgendado.Executar();
            lblDestino.Text = caminho == null
                ? "Falha ao criar o backup. Verifique a pasta destino."
                : "Backup automático criado em:" + Environment.NewLine + caminho;
            CarregarConfig();
        }

        private void FazerBackup()
        {
            if (!File.Exists(Database.CaminhoBanco))
            {
                MessageBox.Show("Banco ainda não criado.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var dlg = new SaveFileDialog
            {
                Filter = "SQLite (*.db)|*.db|Todos (*.*)|*.*",
                FileName = "soen_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".db",
                Title = "Salvar backup do banco"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                File.Copy(Database.CaminhoBanco, dlg.FileName, true);
                lblDestino.Text = "Backup criado com sucesso em:" + Environment.NewLine + dlg.FileName;
            }
        }

        private void Restaurar()
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = "SQLite (*.db)|*.db|Todos (*.*)|*.*",
                Title = "Selecionar arquivo de backup"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                if (MessageBox.Show("Restaurar o banco a partir deste backup?\r\nO banco atual será substituído.", "Confirmar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                File.Copy(dlg.FileName, Database.CaminhoBanco, true);
                lblDestino.Text = "Banco restaurado com sucesso.\r\nReinicie o sistema para carregar os dados.";
            }
        }
    }
}