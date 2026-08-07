using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Soen___Torrezim.Data;

namespace Soen___Torrezim
{
    public partial class FrmPrincipal : Form
    {
        private System.Windows.Forms.Timer _timerBackup;
        public FrmPrincipal()
        {
            InitializeComponent();
            var itemInicio = new ToolStripMenuItem("Painel Inicial (Dashboard)");
            itemInicio.Click += (s, e) => new DashboardPrincipal().Show();
            menuStrip1.Items.Insert(0, itemInicio);

            var itemServicos = new ToolStripMenuItem("Cadastro de Serviços");
            itemServicos.Click += (s, e) => new CadastroServico().Show();
            toolStripMenuItem5.DropDownItems.Add(itemServicos);

            var itemTecnicos = new ToolStripMenuItem("Técnicos");
            itemTecnicos.Click += (s, e) => new Tecnicos().Show();
            toolStripMenuItem5.DropDownItems.Add(itemTecnicos);

            var itemComTecnico = new ToolStripMenuItem("Comissões por Técnico");
            itemComTecnico.Click += (s, e) => new RelatorioTecnico().Show();
            toolStripMenuItem5.DropDownItems.Add(itemComTecnico);

            var itemTempoTecnico = new ToolStripMenuItem("Tempo Médio de Atendimento por Técnico");
            itemTempoTecnico.Click += (s, e) => new RelatorioTempoTecnico().Show();
            toolStripMenuItem5.DropDownItems.Add(itemTempoTecnico);

            var itemPagamentoComissao = new ToolStripMenuItem("Pagamentos de Comissões");
            itemPagamentoComissao.Click += (s, e) => new ComissoesPagamentos().Show();
            toolStripMenuItem5.DropDownItems.Add(itemPagamentoComissao);

            var itemMargem = new ToolStripMenuItem("Margem por Serviço/Peça");
            itemMargem.Click += (s, e) => new RelatorioMargem().Show();
            toolStripMenuItem8.DropDownItems.Add(itemMargem);

            var itemBackup = new ToolStripMenuItem("Backup e Restauração");
            itemBackup.Click += (s, e) => new BackupRestore().Show();
            toolStripMenuItem7.DropDownItems.Add(itemBackup);

            var itemUsuarios = new ToolStripMenuItem("Usuários do Sistema");
            itemUsuarios.Click += (s, e) => new Usuarios().ShowDialog();
            toolStripMenuItem7.DropDownItems.Add(itemUsuarios);

            var itemEmpresa = new ToolStripMenuItem("Dados da Empresa");
            itemEmpresa.Click += (s, e) => new EmpresaConfig().ShowDialog();
            toolStripMenuItem7.DropDownItems.Add(itemEmpresa);

            var itemTrocar = new ToolStripMenuItem("Trocar Usuário");
            itemTrocar.Click += (s, e) => TrocarUsuario();
            toolStripMenuItem7.DropDownItems.Add(itemTrocar);
        }

        private void TrocarUsuario()
        {
            Hide();
            using (var login = new Login())
            {
                if (login.ShowDialog() != DialogResult.OK)
                {
                    Close();
                    return;
                }
            }
            Show();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
        }

        private void cadastroDeClientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new CadastroCliente().Show();
        }

        private void consultaDeClientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ConsultaCliente().Show();
        }

        private void ediçãoDeDadosDosClientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ConsultaCliente().Show();
        }

        private void exclusãoDeClientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ConsultaCliente().Show();
        }

        private void históricoDeComprasDosClientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new HistoricoCompraCliente().Show();
        }

        private void cadastroDeVeículosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new CadastroVeiculo().Show();
        }

        private void manutençõesDosVeículosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ManutecaoVeiculo().Show();
        }

        private void históricoDeManutençõesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new HistoricoManutencao().Show();
        }

        private void descriçãoDetalhadaDeCadaVeículoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new DescricaoVeiculo().Show();
        }

        private void fornecedoresECredoresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new CadastroFornecedor().Show();
        }

        private void contasAPagarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ContasPagar().Show();
        }

        private void contasAReceberToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ContasReceber().Show();
        }

        private void relatóriosDeFornecedoresECredoresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new RelatorioFC().Show();
        }

        private void registroDeVendasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new RegistroVendas().Show();
        }

        private void controleDeCaixaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ControleCaixa().Show();
        }

        private void análiseDeMargemDeLucroToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AnaliseLucro().Show();
        }

        private void relatóriosDeVendasEFinanceirosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new RelatorioVFi().Show();
        }

        private void agendamentoDeServiçosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AgendamentoServicos().Show();
        }

        private void criaçãoDeOrçamentosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new CriacaoOrcamentos().Show();
        }

        private void acompanhamentoDeServiçosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AcompanhamentoS().Show();
        }

        private void relatóriosDeServiçosPrestadoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new RelatorioSP().Show();
        }

        private void calendárioDeAgendamentosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new CalendariosAgen().Show();
        }

        private void gerenciamentoDeHoráriosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new GerenciamentoH().Show();
        }

        private void notificaçõesDeAgendamentosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new NotificaoAgen().Show();
        }

        private void confirmaçãoDeServiçosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ConfirmacaoS().Show();
        }

        private void entradaDeEstoqueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new EntradaEstoque().Show();
        }

        private void saídaDeEstoqueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new SaidaEs().Show();
        }

        private void inventárioDePeçasEMateriaisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new InventarioPM().Show();
        }

        private void relatóriosDeEstoqueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new InventarioPM().Show();
        }

        private void soenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox1().Show();
        }

        private void deepDarknessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new DeepDarkness().Show();
        }

        private void erikaLellisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ErikaLelliscs().Show();
        }

        private void atualizacoesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Atualizar().Show();
        }

        private void relatóriosGerenciaisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new RelatorioAnalise().Show();
        }

        private void análisesDeDesempenhoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new RelatorioAnalise().Show();
        }

        private void estatísticasDeVendasEServiçosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new RelatorioAnalise().Show();
        }

        private void relatóriosPersonalizadosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new RelatorioAnalise().Show();
        }

        private void configuraçãoDeNotificaçõesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new NotificaoAgen().Show();
        }

        private void lembretesDeTarefasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new NotLemb().Show();
        }

        private void alertasDeVencimentosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ContasPagar().Show();
        }

        private void notificaçõesPersonalizadasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new NotificaoAgen().Show();
        }

        private void exportaçãoDeDadosContábeisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new IntegraContabil().Show();
        }

        private void relatóriosFiscaisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new IntegraContabil().Show();
        }

        private void conferênciaComAContabilidadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new IntegraContabil().Show();
        }

        private void integraçãoDeSistemasContábeisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new IntegraContabil().Show();
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
        }

        private void toolStripProgressBar1_Click(object sender, EventArgs e)
        {
        }

        private void FrmPrincipal_Load(object sender, EventArgs e)
        {
            BackupAgendado.VerificarAgenda();
            _timerBackup = new System.Windows.Forms.Timer { Interval = 3600000 };
            _timerBackup.Tick += (s, ev) => BackupAgendado.VerificarAgenda();
            _timerBackup.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { BackupAgendado.VerificarAgenda(true); } catch { }
            base.OnFormClosing(e);
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (!statusStrip1.Items.Contains(toolStripStatusLabel1))
                statusStrip1.Items.Add(toolStripStatusLabel1);
        }

        private void sairmenu_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}