using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>
    /// Sessão global do sistema: guarda o usuário autenticado e as
    /// autorizações de perfil (admin / operador).
    /// </summary>
    public static class Sessao
    {
        public static Usuario UsuarioAtual { get; set; }

        public static bool EhAdmin
        {
            get { return UsuarioAtual != null && UsuarioAtual.Perfil == "admin"; }
        }

        public static bool PrepararExclusao()
        {
            if (EhAdmin) return true;
            System.Windows.Forms.MessageBox.Show(
                "Somente administradores podem excluir registros.",
                "Acesso restrito", System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Warning);
            return false;
        }
    }
}