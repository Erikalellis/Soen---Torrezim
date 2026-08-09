using System;
using System.Windows.Forms;
using Soen___Torrezim.Data;

namespace Soen___Torrezim
{
    static class Program
    {
        /// <summary>Ponto de entrada principal para o aplicativo.</summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Database.Inicializar();
            UsuarioDAO.GarantirAdminPadrao();

            if (!Database.BancoIntegro())
            {
                var resposta = MessageBox.Show(
                    "O banco de dados (soen.db) parece estar corrompido.\n\n" +
                    "Deseja restaurar o backup automático mais recente?",
                    "Banco de dados", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                if (resposta == DialogResult.Yes)
                {
                    string restaurado = BackupAgendado.RestaurarUltimo();
                    if (restaurado != null)
                    {
                        MessageBox.Show("Banco restaurado a partir de:\n" + restaurado,
                            "Restauração", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Não foi possível restaurar. Verifique a pasta de Backups " +
                            "ou restaure manualmente em Configurações → Backup e Restauração.",
                            "Restauração", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }

            using (var login = new Login())
            {
                if (login.ShowDialog() != DialogResult.OK)
                    return; // usuário cancelou ou não autenticou
                Sessao.UsuarioAtual = login.UsuarioLogado;
            }

            Application.Run(new FrmPrincipal());
        }
    }
}