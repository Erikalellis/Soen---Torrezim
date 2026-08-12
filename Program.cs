using System;
using System.Threading;
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

            using (var mutex = new Mutex(true, @"Local\Soen_Torrezim_SingleInstance", out bool criadoNovo))
            {
                if (!criadoNovo)
                {
                    MessageBox.Show("O SOEN já está em execução.", "SOEN",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                ExecutarAplicativo();
            }
        }

        private static void ExecutarAplicativo()
        {
            Application.ThreadException += (s, e) => TratarExcecaoNaoTratada(e.Exception);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                TratarExcecaoNaoTratada(e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()));

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

        private static void TratarExcecaoNaoTratada(Exception ex)
        {
            try
            {
                Logger.LogError(ex);
                MessageBox.Show(
                    "Ocorreu um erro inesperado. Os detalhes foram registrados em logs\\soen.log.\n\n" + ex.Message,
                    "SOEN - Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
                // nunca deixar o tratador global falhar
            }
        }
    }
}