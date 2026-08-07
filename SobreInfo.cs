using System;
using System.Reflection;
using System.Windows.Forms;
using Soen___Torrezim.Data;

namespace Soen___Torrezim
{
    /// <summary>Tela "Sobre / Informações" com dados do sistema, da empresa e créditos.</summary>
    public partial class SobreInfo : Form
    {
        public SobreInfo()
        {
            InitializeComponent();

            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            var e = EmpresaDAO.Obter();

            var texto = new System.Text.StringBuilder();
            texto.AppendLine("SOEN - Sistema de Ordem de Serviço");
            texto.AppendLine();
            texto.AppendLine("Versão: " + (ver != null ? ver.ToString() : "?") + "  (Compilação: " + ver.Build + ")");
            texto.AppendLine();
            texto.AppendLine("Desenvolvido por: Erika Lellis (Deep Darkness)");
            texto.AppendLine();
            texto.AppendLine("Banco de dados:" + Environment.NewLine + (Database.CaminhoBanco ?? "não informado"));
            texto.AppendLine();
            texto.AppendLine("--- Dados da Empresa ---");
            texto.AppendLine("Nome: " + e.Nome);
            texto.AppendLine("CNPJ: " + e.Cnpj);
            texto.AppendLine("Telefone: " + e.Telefone);
            texto.AppendLine("Endereço: " + e.Endereco);
            texto.AppendLine("Cidade/UF: " + e.Cidade + " - " + e.Estado);
            texto.AppendLine("E-mail: " + e.Email);
            texto.AppendLine("Site: " + e.Site);
            texto.AppendLine();
            texto.AppendLine("Contato / Suporte: " + e.Telefone);
            texto.AppendLine(e.Observacoes);

            lblInfo.Text = texto.ToString();
        }

        private void btnFechar_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}