using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>
    /// Painel inicial (Dashboard): resume o dia da oficina com OS em aberto,
    /// vendas, caixa, contas a vencer, revisões vencidas/próximas e estoque
    /// baixo, mais uma lista das pendências críticas.
    /// </summary>
    public partial class DashboardPrincipal : Form
    {
        private Label lblOsAbertas;
        private Label lblVendasHoje;
        private Label lblSaldoCaixa;
        private Label lblReceber;
        private Label lblRevisoes;
        private Label lblEstoqueBaixo;
        private Label lblFaturamentoMes;
        private Label lblComissoes;
        private DataGridView gridPend;
        private Button btnAtualizar;

        public DashboardPrincipal()
        {
            InitializeComponent();
            Text = "Soen - Painel Inicial";
            ClientSize = new Size(860, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
        }

        private void CriarInterface()
        {
            btnAtualizar = UIHelpers.CreateButton("Atualizar", new Point(12, 12), new Size(110, 30));
            btnAtualizar.Click += (s, e) => Carregar();

            var titulo = new Label
            {
                Text = "Painel Inicial — Oficina",
                AutoSize = true,
                Location = new Point(12, 50),
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };

            int w = 200, h = 70;
            Point[] pos = { new Point(12, 96), new Point(230, 96), new Point(448, 96), new Point(660, 96),
                            new Point(12, 182), new Point(230, 182), new Point(448, 182), new Point(660, 182) };

            lblOsAbertas       = CriarCard(pos[0], w, h);
            lblVendasHoje      = CriarCard(pos[1], w, h);
            lblSaldoCaixa      = CriarCard(pos[2], w, h);
            lblReceber         = CriarCard(pos[3], w, h);
            lblFaturamentoMes  = CriarCard(pos[4], w, h);
            lblComissoes       = CriarCard(pos[5], w, h);
            lblRevisoes        = CriarCard(pos[6], w, h);
            lblEstoqueBaixo    = CriarCard(pos[7], w, h);

            var lblPend = new Label { Text = "Pendências críticas:", AutoSize = true, Location = new Point(12, 282), Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            gridPend = new DataGridView
            {
                Location = new Point(12, 308),
                Size = new Size(860, 220),
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            gridPend.Columns.Add("Tipo", "Assunto");
            gridPend.Columns.Add("Detalhe", "Detalhe");
            gridPend.Columns["Tipo"].FillWeight = 1.2f;
            gridPend.Columns["Detalhe"].FillWeight = 3f;

            Controls.AddRange(new Control[] { btnAtualizar, titulo,
                lblOsAbertas, lblVendasHoje, lblSaldoCaixa, lblReceber,
                lblFaturamentoMes, lblComissoes, lblRevisoes, lblEstoqueBaixo,
                lblPend, gridPend });
        }

        private Label CriarCard(Point p, int w, int h)
        {
            var l = new Label
            {
                Location = p,
                Size = new Size(w, h),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = SystemColors.Window,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8)
            };
            return l;
        }

        private void Carregar()
        {
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            List<Orcamento> orcs = ServicoDAO.ListarOrcamentos();
            List<Venda> vendas = VendaDAO.Listar(null);
            List<LancamentoCaixa> caixa = CaixaDAO.Listar();
            List<ContaFinanceira> contas = FinanceiroDAO.Listar("");
            List<Veiculo> veiculos = VeiculoDAO.Listar(null);
            List<Produto> produtos = ProdutoDAO.Listar();
            List<Cliente> clientes = ClienteDAO.Listar("");

            string hoje = DateTime.Now.ToString("yyyy-MM-dd");

            // OS em aberto
            int osAbertas = 0; double valorOs = 0;
            foreach (var o in orcs) if (o.Status == "em_aberto") { osAbertas++; valorOs += o.Valor; }
            lblOsAbertas.Text = "OS / Orçamentos em aberto\n" + osAbertas + " — R$ " + valorOs.ToString("N2", cult);

            // Vendas hoje
            int vendasHoje = 0; double totalHoje = 0;
            foreach (var v in vendas) if ((v.Data ?? "").StartsWith(hoje)) { vendasHoje++; totalHoje += v.ValorTotal; }
            lblVendasHoje.Text = "Vendas hoje\n" + vendasHoje + " — R$ " + totalHoje.ToString("N2", cult);

            // Saldo caixa + status aberto/fechado
            double saldo = 0;
            foreach (var c in caixa) saldo += (c.Tipo == "saida" ? -c.Valor : c.Valor);
            string statusCaixa = ConfigDAO.Obter("caixa_fechado", "") == hoje ? "FECHADO" : "aberto";
            lblSaldoCaixa.Text = "Caixa (" + statusCaixa + ")\nR$ " + saldo.ToString("N2", cult);

            // Contas a receber em aberto + a vencer nos próximos 7 dias
            double aReceber = 0; int qtReceber = 0;
            int aVencer7Dias = 0;
            foreach (var c in contas)
            {
                if (c.Status == "pago" || c.Status == "cancelado") continue;
                DateTime dv;
                if (c.Tipo == "receber") { aReceber += c.Valor; qtReceber++; }
                if (DateTime.TryParse(c.Vencimento, out dv) &&
                    dv.Date >= DateTime.Today && dv.Date <= DateTime.Today.AddDays(7))
                    aVencer7Dias++;
            }
            lblReceber.Text = "Contas a receber (aberto)\n" + qtReceber + " — R$ " + aReceber.ToString("N2", cult) +
                "\n• " + aVencer7Dias + " a vencer em 7 dias";

            // Faturamento do mês (todas as vendas do mês atual, incluindo OS convertidas)
            string mesAtual = DateTime.Now.ToString("yyyy-MM");
            double faturamentoMes = 0;
            foreach (var v in vendas) if ((v.Data ?? "").StartsWith(mesAtual)) faturamentoMes += v.ValorTotal;
            lblFaturamentoMes.Text = "Faturamento do mês\nR$ " + faturamentoMes.ToString("N2", cult);

            // Comissões a pagar (saldo gerado − pago pelos técnicos)
            double comissoesPagar = 0;
            foreach (var resumo in TecnicoDAO.ListarResumoComissoes())
                if (resumo.Saldo > 0) comissoesPagar += resumo.Saldo;
            lblComissoes.Text = "Comissões a pagar\nR$ " + comissoesPagar.ToString("N2", cult);

            // Revisões
            int revisoes = 0;
            foreach (var v in veiculos)
            {
                DateTime d;
                if (!string.IsNullOrWhiteSpace(v.ProximaRevisao) && DateTime.TryParse(v.ProximaRevisao, out d) && d.Date < DateTime.Today.AddDays(16))
                    revisoes++;
            }
            lblRevisoes.Text = "Revisões/garantia/IPVA a vencer\n" + revisoes + " veículo(s)";

            // Estoque baixo
            int baixo = 0;
            foreach (var p in produtos) if (p.QtdAtual <= 5) baixo++;
            lblEstoqueBaixo.Text = "Produtos com estoque baixo\n" + baixo + " item(ns)  (≤5)";

            CarregarPendencias(orcs, veiculos, contas, produtos);
        }

        private void CarregarPendencias(List<Orcamento> orcs, List<Veiculo> veiculos, List<ContaFinanceira> contas, List<Produto> produtos)
        {
            gridPend.Rows.Clear();

            // OS aprovadas aguardando conversão
            foreach (var o in orcs)
                if (o.Status == "aprovado")
                    gridPend.Rows.Add("OS aprovada", (string.IsNullOrWhiteSpace(o.Numero) ? "#" + o.Id : o.Numero) + " — " + o.NomeCliente + " (aguarda conversão em venda)");

            // Revisões vencidas
            foreach (var v in veiculos)
            {
                DateTime d;
                if (!string.IsNullOrWhiteSpace(v.ProximaRevisao) && DateTime.TryParse(v.ProximaRevisao, out d) && d.Date < DateTime.Today)
                    gridPend.Rows.Add("Revisão vencida", v.Placa + " — vencida em " + d.ToString("dd/MM/yyyy"));
            }

            // Alertas de vencimento (garantia / IPVA / licenciamento) — vencidos ou a vencer em 30 dias
            foreach (var v in veiculos)
            {
                AlertarVencimento(v, v.GarantiaFim, "Garantia", "garantia termina");
                AlertarVencimento(v, v.IpvaVenc, "IPVA", "IPVA vence");
                AlertarVencimento(v, v.LicenciamentoVenc, "Licenciamento", "licenciamento vence");
            }

            // Contas a pagar/receber em aberto — vencidas ou a vencer em 15 dias
            foreach (var c in contas)
            {
                if (c.Status == "pago" || c.Status == "cancelado") continue;
                DateTime d;
                if (!DateTime.TryParse(c.Vencimento, out d)) continue;
                string rotulo = c.Tipo == "pagar" ? "Conta a pagar" : "Conta a receber";
                if (d.Date < DateTime.Today)
                    gridPend.Rows.Add(rotulo + " vencida",
                        c.Descricao + " — venceu em " + d.ToString("dd/MM/yyyy"));
                else if (d.Date <= DateTime.Today.AddDays(15))
                {
                    int dt = (d.Date - DateTime.Today.Date).Days;
                    gridPend.Rows.Add(rotulo + " vence em " + dt + " dia" + (dt == 1 ? "" : "s"),
                        c.Descricao + " — " + d.ToString("dd/MM/yyyy") +
                        " (R$ " + c.Valor.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")) + ")");
                }
            }

            // Estoque baixo
            foreach (var p in produtos)
                if (p.QtdAtual <= 5)
                    gridPend.Rows.Add("Estoque baixo", p.Nome + " — qtd: " + p.QtdAtual.ToString("0.##") + " " + p.Unidade);
        }

        private void AlertarVencimento(Veiculo v, string dataStr, string rotulo, string verbo)
        {
            if (string.IsNullOrWhiteSpace(dataStr)) return;
            DateTime d;
            if (!DateTime.TryParse(dataStr, out d)) return;
            if (d.Date < DateTime.Today.AddDays(30))
                gridPend.Rows.Add(rotulo + (d.Date < DateTime.Today ? " vencido" : " a vencer"),
                    v.Placa + " — " + verbo + " em " + d.ToString("dd/MM/yyyy"));
        }
    }
}