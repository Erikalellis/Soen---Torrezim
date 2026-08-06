using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Soen___Torrezim.Data;

namespace Soen___Torrezim
{
    /// <summary>Backup e restauração do banco de dados (arquivo SQLite).</summary>
    public partial class BackupRestore : Form
    {
        private Label lblInfo;
        private Label lblDestino;
        private Button btnBackup;
        private Button btnRestore;

        public BackupRestore()
        {
            InitializeComponent();
            Text = "Soen - Backup e Restauração";
            ClientSize = new Size(520, 260);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
        }

        private void CriarInterface()
        {
            var l1 = new Label { Text = "Arquivo do banco:", AutoSize = true, Location = new Point(12, 20) };
            lblInfo = new Label { Text = Database.CaminhoBanco, Location = new Point(12, 40), Size = new Size(490, 40) };
            lblDestino = new Label { Text = "", Location = new Point(12, 110), Size = new Size(490, 60) };

            btnBackup = UIHelpers.CreateButton("Fazer Backup...", new Point(12, 170), new Size(130, 28));
            btnBackup.Click += (s, e) => FazerBackup();

            btnRestore = UIHelpers.CreateButton("Restaurar Backup...", new Point(160, 170), new Size(140, 28));
            btnRestore.Click += (s, e) => Restaurar();

            Controls.AddRange(new Control[] { l1, lblInfo, lblDestino, btnBackup, btnRestore });
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