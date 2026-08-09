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