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
        private ToolStripMenuItem _itemConfig;
        public FrmPrincipal()
        {
            InitializeComponent();
            var itemInicio = new ToolStripMenuItem("Painel Inicial");
            itemInicio.Click += (s, e) => Janelas.Abrir(() => new DashboardPrincipal());

            var itemServicos = new ToolStripMenuItem("Cadastro de Serviços");
            itemServicos.Click += (s, e) => Janelas.Abrir(() => new CadastroServico());
            toolStripMenuItem5.DropDownItems.Add(itemServicos);

            var itemTecnicos = new ToolStripMenuItem("Técnicos");
            itemTecnicos.Click += (s, e) => Janelas.Abrir(() => new Tecnicos());
            toolStripMenuItem5.DropDownItems.Add(itemTecnicos);

            var itemComTecnico = new ToolStripMenuItem("Comissões por Técnico");
            itemComTecnico.Click += (s, e) => Janelas.Abrir(() => new RelatorioTecnico());
            toolStripMenuItem5.DropDownItems.Add(itemComTecnico);

            var itemTempoTecnico = new ToolStripMenuItem("Tempo Médio de Atendimento por Técnico");
            itemTempoTecnico.Click += (s, e) => Janelas.Abrir(() => new RelatorioTempoTecnico());
            toolStripMenuItem5.DropDownItems.Add(itemTempoTecnico);

            var itemPagamentoComissao = new ToolStripMenuItem("Pagamentos de Comissões");
            itemPagamentoComissao.Click += (s, e) => Janelas.Abrir(() => new ComissoesPagamentos());
            toolStripMenuItem5.DropDownItems.Add(itemPagamentoComissao);

            // == Consolidado em "Outros > Relatórios e Análises" ==
            toolStripMenuItem8.DropDownItems.Clear();
            var itemAnalise = new ToolStripMenuItem("Análise Gerencial");
            itemAnalise.Click += (s, e) => Janelas.Abrir(() => new RelatorioAnalise());
            toolStripMenuItem8.DropDownItems.Add(itemAnalise);
            var itemMargem = new ToolStripMenuItem("Margem por Serviço/Peça");
            itemMargem.Click += (s, e) => Janelas.Abrir(() => new RelatorioMargem());
            toolStripMenuItem8.DropDownItems.Add(itemMargem);

            // ===== Menu Configurações (consolida as preferências do sistema) =====
            var itemConfig = new ToolStripMenuItem("Configurações");
            _itemConfig = itemConfig;

            var itemEmpresa = new ToolStripMenuItem("Dados da Empresa");
            itemEmpresa.Click += (s, e) => new EmpresaConfig().ShowDialog();
            itemConfig.DropDownItems.Add(itemEmpresa);

            var itemImpressora = new ToolStripMenuItem("Impressora");
            itemImpressora.Click += (s, e) => Janelas.Abrir(() => new ConfigImpressora());
            itemConfig.DropDownItems.Add(itemImpressora);

            itemConfig.DropDownItems.Add(new ToolStripSeparator());

            var itemBackup = new ToolStripMenuItem("Backup e Restauração");
            itemBackup.Click += (s, e) => Janelas.Abrir(() => new BackupRestore());
            itemConfig.DropDownItems.Add(itemBackup);

            var itemUsuarios = new ToolStripMenuItem("Usuários do Sistema");
            itemUsuarios.Click += (s, e) => new Usuarios().ShowDialog();
            itemConfig.DropDownItems.Add(itemUsuarios);

            var itemTrocar = new ToolStripMenuItem("Trocar Usuário");
            itemTrocar.Click += (s, e) => TrocarUsuario();
            itemConfig.DropDownItems.Add(itemTrocar);

            itemConfig.DropDownItems.Add(new ToolStripSeparator());

            var itemSobre = new ToolStripMenuItem("Sobre / Informações");
            itemSobre.Click += (s, e) => new SobreInfo().ShowDialog();
            itemConfig.DropDownItems.Add(itemSobre);

            // ===== Menu SoenWebApi (WhatsApp) =====
            var itemWebApi = new ToolStripMenuItem("SoenWebApi");
            var itemWebStatus = new ToolStripMenuItem("Verificar Status");
            itemWebStatus.Click += (s, e) => VerificarStatusWebApi();
            var itemWebPainel = new ToolStripMenuItem("Abrir Painel no Navegador");
            itemWebPainel.Click += (s, e) => AbrirPainelWebApi();
            var itemWebDocs = new ToolStripMenuItem("Abrir Documentação da API");
            itemWebDocs.Click += (s, e) => AbrirDocsWebApi();
            itemWebApi.DropDownItems.Add(new ToolStripSeparator());
            var itemWebIniciar = new ToolStripMenuItem("Iniciar SoenWebApi");
            itemWebIniciar.Click += (s, e) => IniciarWebApi();
            var itemWebParar = new ToolStripMenuItem("Parar SoenWebApi");
            itemWebParar.Click += (s, e) => PararWebApi();
            itemWebApi.DropDownItems.AddRange(new ToolStripItem[]
            {
                itemWebStatus, itemWebIniciar, itemWebParar,
                new ToolStripSeparator(), itemWebPainel, itemWebDocs
            });

            // ===== Menu Sair (fecha o programa) =====
            var itemSair = new ToolStripMenuItem("Sair");
            itemSair.Click += (s, e) => Close();

            // ===== Pesquisa global (Ctrl+F) =====
            var itemPesquisar = new ToolStripMenuItem("Pesquisar");
            itemPesquisar.ShortcutKeys = Keys.Control | Keys.F;
            itemPesquisar.Click += (s, e) => Janelas.Abrir(() => new PesquisaGlobal());

// Ordem da barra: Painel Inicial (Dashboard) no início; Pesquisar logo em
            // seguida; "Configurações" e "Sair" ficam após "Serviços e Orçamentos".
            menuStrip1.Items.Insert(0, itemInicio);
            menuStrip1.Items.Insert(1, itemPesquisar);
            menuStrip1.Items.Insert(7, itemWebApi);
            menuStrip1.Items.Insert(8, itemConfig);
            menuStrip1.Items.Insert(9, itemSair);

            ConsolidarMenuOutros();
            AplicarPermissoes();
        }

        /// <summary>Une os itens repetidos que abriam módulos iguais no menu "Outros".</summary>
        private void ConsolidarMenuOutros()
        {
            toolStripMenuItem8.DropDownItems.Remove(relatóriosGerenciaisToolStripMenuItem);
            toolStripMenuItem8.DropDownItems.Remove(análisesDeDesempenhoToolStripMenuItem);
            toolStripMenuItem8.DropDownItems.Remove(estatísticasDeVendasEServiçosToolStripMenuItem);
            toolStripMenuItem8.DropDownItems.Remove(relatóriosPersonalizadosToolStripMenuItem);

            toolStripMenuItem13.DropDownItems.Remove(configuraçãoDeNotificaçõesToolStripMenuItem);
            toolStripMenuItem13.DropDownItems.Remove(notificaçõesPersonalizadasToolStripMenuItem);

            toolStripMenuItem14.DropDownItems.Remove(relatóriosFiscaisToolStripMenuItem);
            toolStripMenuItem14.DropDownItems.Remove(conferênciaComAContabilidadeToolStripMenuItem);
            toolStripMenuItem14.DropDownItems.Remove(integraçãoDeSistemasContábeisToolStripMenuItem);

            // Remove "Relatórios de Estoque" (duplicava o Inventário) e o "Sair" interno,
            // e o menu stub "Deep Darkness" (o "Sobre" ficou em Configurações).
            toolStripMenuItem6.DropDownItems.Remove(relatóriosDeEstoqueToolStripMenuItem);
            toolStripMenuItem7.DropDownItems.Remove(sairmenu);
            menuStrip1.Items.Remove(toolStripMenuItem12);
        }

        /// <summary>Aplica as permissões por perfil: só admin acessa usuários, backup e contábil.</summary>
        private void AplicarPermissoes()
        {
            bool admin = Sessao.EhAdmin;
            if (_itemConfig != null)
            {
                foreach (ToolStripItem it in _itemConfig.DropDownItems)
                {
                    if (it.Text.StartsWith("Usuários") || it.Text.StartsWith("Backup"))
                        it.Enabled = admin;
                }
            }
            toolStripMenuItem14.Enabled = admin; // integração com a contabilidade

if (Sessao.UsuarioAtual != null)
                toolStripStatusLabel1.Text = "Usuário: " + Sessao.UsuarioAtual.Nome +
                    (admin ? " (administrador)" : " (operador)");
        }

private async void VerificarStatusWebApi()
        {
            string pasta = SoenWebApiManager.Localizar();
            if (pasta == null)
            {
                MessageBox.Show("A SoenWebApi (WhatsApp) não foi instalada neste computador.\n" +
                    "Reinstale o SOEN marcando a opção \"SoenWebApi\".",
                    "SoenWebApi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            bool online = await SoenWebApiManager.EstahOnlineAsync();
            string msg = online
                ? "SoenWebApi: ONLINE.\n\nURL: " + SoenWebApiManager.UrlBase + "\nPainel: " + SoenWebApiManager.UrlPainel
                : "SoenWebApi: OFFLINE.\n\nO serviço não está respondendo.\nUse \"Iniciar SoenWebApi\" para iniciar.";
            MessageBox.Show(msg, "SoenWebApi - Status", MessageBoxButtons.OK,
                online ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void AbrirPainelWebApi()
        {
            if (SoenWebApiManager.Localizar() == null)
            {
                MessageBox.Show("A SoenWebApi não foi instalada.\nReinstale o SOEN marcando a opção \"SoenWebApi\".",
                    "SoenWebApi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SoenWebApiManager.AbrirNavegador(SoenWebApiManager.UrlPainel);
        }

        private void AbrirDocsWebApi()
        {
            if (SoenWebApiManager.Localizar() == null)
            {
                MessageBox.Show("A SoenWebApi não foi instalada.\nReinstale o SOEN marcando a opção \"SoenWebApi\".",
                    "SoenWebApi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SoenWebApiManager.AbrirNavegador(SoenWebApiManager.UrlDocs);
        }

        private void IniciarWebApi()
        {
            if (!SoenWebApiManager.Iniciar())
            {
                MessageBox.Show("Não foi possível iniciar a SoenWebApi.\nConfirme se ela foi instalada (pasta SoenWebApi ao lado do aplicativo).",
                    "SoenWebApi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            MessageBox.Show("SoenWebApi iniciando em segundo plano...\n\n" +
                "Aguarde alguns segundos e verifique o Status ou abra o Painel para conectar o WhatsApp.",
                "SoenWebApi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void PararWebApi()
        {
            SoenWebApiManager.Parar();
            MessageBox.Show("SoenWebApi encerrada.", "SoenWebApi", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                Sessao.UsuarioAtual = login.UsuarioLogado;
            }
            AplicarPermissoes();
            Show();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
        }

        private void cadastroDeClientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new CadastroCliente());
        }

        private void consultaDeClientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new ConsultaCliente());
        }

        private void ediçãoDeDadosDosClientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new ConsultaCliente());
        }

        private void exclusãoDeClientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new ConsultaCliente());
        }

        private void históricoDeComprasDosClientesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new HistoricoCompraCliente());
        }

        private void cadastroDeVeículosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new CadastroVeiculo());
        }

        private void manutençõesDosVeículosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new ManutecaoVeiculo());
        }

        private void históricoDeManutençõesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new HistoricoManutencao());
        }

        private void descriçãoDetalhadaDeCadaVeículoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new DescricaoVeiculo());
        }

        private void fornecedoresECredoresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new CadastroFornecedor());
        }

        private void contasAPagarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new ContasPagar());
        }

        private void contasAReceberToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new ContasReceber());
        }

        private void relatóriosDeFornecedoresECredoresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new RelatorioFC());
        }

        private void registroDeVendasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new RegistroVendas());
        }

        private void controleDeCaixaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new ControleCaixa());
        }

        private void análiseDeMargemDeLucroToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new AnaliseLucro());
        }

        private void relatóriosDeVendasEFinanceirosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new RelatorioVFi());
        }

        private void agendamentoDeServiçosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new AgendamentoServicos());
        }

        private void criaçãoDeOrçamentosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new CriacaoOrcamentos());
        }

        private void acompanhamentoDeServiçosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new AcompanhamentoS());
        }

        private void relatóriosDeServiçosPrestadoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new RelatorioSP());
        }

        private void calendárioDeAgendamentosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new CalendariosAgen());
        }

        private void gerenciamentoDeHoráriosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new GerenciamentoH());
        }

        private void notificaçõesDeAgendamentosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new NotificaoAgen());
        }

        private void confirmaçãoDeServiçosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new ConfirmacaoS());
        }

        private void entradaDeEstoqueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new EntradaEstoque());
        }

        private void saídaDeEstoqueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new SaidaEs());
        }

        private void inventárioDePeçasEMateriaisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new InventarioPM());
        }

        private void relatóriosDeEstoqueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new InventarioPM());
        }

        private void soenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new AboutBox1());
        }

        private void deepDarknessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new DeepDarkness());
        }

        private void erikaLellisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new ErikaLelliscs());
        }

        private void atualizacoesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new Atualizar());
        }

        private void relatóriosGerenciaisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new RelatorioAnalise());
        }

        private void análisesDeDesempenhoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new RelatorioAnalise());
        }

        private void estatísticasDeVendasEServiçosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new RelatorioAnalise());
        }

        private void relatóriosPersonalizadosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new RelatorioAnalise());
        }

        private void configuraçãoDeNotificaçõesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new NotificaoAgen());
        }

        private void lembretesDeTarefasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new NotLemb());
        }

        private void alertasDeVencimentosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new ContasPagar());
        }

        private void notificaçõesPersonalizadasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new NotificaoAgen());
        }

        private void exportaçãoDeDadosContábeisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new IntegraContabil());
        }

        private void relatóriosFiscaisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new IntegraContabil());
        }

        private void conferênciaComAContabilidadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new IntegraContabil());
        }

        private void integraçãoDeSistemasContábeisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Janelas.Abrir(() => new IntegraContabil());
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
        }

        private void toolStripProgressBar1_Click(object sender, EventArgs e)
        {
        }

        private void FrmPrincipal_Load(object sender, EventArgs e)
        {
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
            toolStripProgressBar1.MarqueeAnimationSpeed = 30;
            var timerCarregando = new System.Windows.Forms.Timer { Interval = 1000 };
            timerCarregando.Tick += (s, ev) =>
            {
                timerCarregando.Stop();
                toolStripProgressBar1.Style = ProgressBarStyle.Blocks;
                toolStripProgressBar1.Value = 0;
            };
            timerCarregando.Start();

            BackupAgendado.VerificarAgenda();
            _timerBackup = new System.Windows.Forms.Timer { Interval = 3600000 };
            _timerBackup.Tick += (s, ev) => BackupAgendado.VerificarAgenda();
            _timerBackup.Start();

            VerificarAtualizacaoAuto();
        }

        /// <summary>Verifica, em segundo plano, se há nova versão e informa o usuário.</summary>
        private async void VerificarAtualizacaoAuto()
        {
            try
            {
                Updater.ReleaseInfo release = null;
                string erro = null;
                bool ok = await System.Threading.Tasks.Task.Run(() =>
                    Updater.VerificarAtualizacao(out release, out erro));
                if (ok && Updater.TemNovidade(release) && !IsDisposed && IsHandleCreated)
                {
                    BeginInvoke(new Action(() =>
                    {
                        var r = MessageBox.Show(this,
                            "Nova versão disponível: " + release.Tag + "\n\nDeseja abrir a tela de atualização?",
                            "Atualização disponível", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (r == DialogResult.Yes) Janelas.Abrir(() => new Atualizar());
                    }));
                }
            }
            catch (Exception ex)
            {
                // falha na verificação não deve interromper o sistema
                Logger.LogError(ex);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { BackupAgendado.VerificarAgenda(true); } catch (Exception ex) { Logger.LogError(ex); }
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