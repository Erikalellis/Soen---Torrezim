using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Soen___Torrezim
{
    public partial class FrmPrincipal: Form
    {
        public FrmPrincipal()
        {
            InitializeComponent();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void cadastroDeClientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CadastroCliente formCadastroClientes = new CadastroCliente();
            formCadastroClientes.Show();
        }

        private void consultaDeClientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConsultaCliente formConsultaClientes = new ConsultaCliente();
            formConsultaClientes.Show();

        }

        private void ediçãoDeDadosDosClientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConsultaCliente formConsultaClientes = new ConsultaCliente();
            formConsultaClientes.Show();
        }

        private void exclusãoDeClientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConsultaCliente formConsultaClientes = new ConsultaCliente();
            formConsultaClientes.Show();
        }

        private void históricoDeComprasDosClientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HistoricoCompraCliente formHistoricoCompraCliente = new HistoricoCompraCliente();
            formHistoricoCompraCliente.Show();

        }

        private void cadastroDeVeículosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CadastroVeiculo formCadastroVeiculo = new CadastroVeiculo();
            formCadastroVeiculo.Show();

        }

        private void manutençõesDosVeículosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ManutecaoVeiculo formManutencaoVeiculo = new ManutecaoVeiculo();
            formManutencaoVeiculo.Show();

        }

        private void históricoDeManutençõesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HistoricoManutencao formHistoricoManutencao = new HistoricoManutencao();
            formHistoricoManutencao.Show();

        }

        private void descriçãoDetalhadaDeCadaVeículoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DescricaoVeiculo formDescricaoVeiculo = new DescricaoVeiculo();
            formDescricaoVeiculo.Show();

        }

        private void fornecedoresECredoresToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void contasAPagarToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void contasAReceberToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void relatóriosDeFornecedoresECredoresToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void registroDeVendasToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void controleDeCaixaToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void análiseDeMargemDeLucroToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void relatóriosDeVendasEFinanceirosToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void agendamentoDeServiçosToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void criaçãoDeOrçamentosToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void acompanhamentoDeServiçosToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void relatóriosDeServiçosPrestadoToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void calendárioDeAgendamentosToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void gerenciamentoDeHoráriosToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void notificaçõesDeAgendamentosToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void confirmaçãoDeServiçosToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void entradaDeEstoqueToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void saídaDeEstoqueToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void inventárioDePeçasEMateriaisToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void relatóriosDeEstoqueToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void soenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 formAboutBox1 = new AboutBox1();
            formAboutBox1.Show();

        }

        private void deepDarknessToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void erikaLellisToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void atualizacoesToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void relatóriosGerenciaisToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void análisesDeDesempenhoToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void estatísticasDeVendasEServiçosToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void relatóriosPersonalizadosToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void configuraçãoDeNotificaçõesToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void lembretesDeTarefasToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void alertasDeVencimentosToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void notificaçõesPersonalizadasToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void exportaçãoDeDadosContábeisToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void relatóriosFiscaisToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void conferênciaComAContabilidadeToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void integraçãoDeSistemasContábeisToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripProgressBar1_Click(object sender, EventArgs e)
        {

        }

        private void FrmPrincipal_Load(object sender, EventArgs e)
        {

            {
                // Adicionando um ToolStripStatusLabel com o nome toolStripStatusLabel1 à StatusStrip
                ToolStripStatusLabel toolStripStatusLabel1 = new ToolStripStatusLabel("Deep_Darknesss");
              
            }
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            statusStrip1.Items.Add(new ToolStripStatusLabel("Deep_Darkness"));
            statusStrip1.Items.Add(toolStripStatusLabel1);  

        }

        private void sairmenu_Click(object sender, EventArgs e)
        {
            this.Close();   
                
        }
    }
}
