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
    /// Histórico completo de um cliente: todas as OS/orçamentos, vendas e
    /// agendamentos, com opções de imprimir e exportar em PDF/Excel.
    /// </summary>
    public class HistoricoCliente : BaseForm
    {
        private readonly Cliente _cliente;
        private TabControl tabs;
        private DataGridView gridOrcamentos;
        private DataGridView gridVendas;
        private DataGridView gridAgendamentos;

        public HistoricoCliente(Cliente cliente)
        {
            _cliente = cliente;
            Text = "Histórico do Cliente" + (cliente != null && !string.IsNullOrWhiteSpace(cliente.NomeRazao)
                ? " — " + cliente.NomeRazao : "");
            ClientSize = new Size(900, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
        }

        private void CriarInterface()
        {
            var btnImprimir = UIHelpers.CreateButton("Imprimir", new Point(12, 12), new Size(100, 30));
            btnImprimir.Click += (s, e) => RelatorioHelper.Imprimir(GridAtual(), "Histórico do Cliente");

            var btnPdf = UIHelpers.CreateButton("Exportar PDF", new Point(120, 12), new Size(110, 30));
            btnPdf.Click += (s, e) => RelatorioHelper.ExportarPdf(GridAtual(), "Histórico do Cliente", "historico_cliente.pdf");

            var btnExcel = UIHelpers.CreateButton("Exportar Excel", new Point(238, 12), new Size(110, 30));
            btnExcel.Click += (s, e) => RelatorioHelper.ExportarExcel(GridAtual(), "historico_cliente.xlsx");

            var btnFechar = UIHelpers.CreateButton("Fechar", new Point(760, 12), new Size(110, 30));
            btnFechar.Click += (s, e) => Close();

            tabs = new TabControl { Location = new Point(12, 52), Size = new Size(876, 490) };

            var tpOrcs = new TabPage("Orçamentos / OS");
            var tpVendas = new TabPage("Vendas");
            var tpAgend = new TabPage("Agendamentos");

            gridOrcamentos = CriarGrid();
            gridVendas = CriarGrid();
            gridAgendamentos = CriarGrid();

            tpOrcs.Controls.Add(gridOrcamentos);
            tpVendas.Controls.Add(gridVendas);
            tpAgend.Controls.Add(gridAgendamentos);

            tabs.TabPages.Add(tpOrcs);
            tabs.TabPages.Add(tpVendas);
            tabs.TabPages.Add(tpAgend);

            Controls.AddRange(new Control[] { btnImprimir, btnPdf, btnExcel, btnFechar, tabs });
        }

        private static DataGridView CriarGrid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            return g;
        }

        private DataGridView GridAtual()
        {
            if (tabs.SelectedTab == tabs.TabPages["Agendamentos"]) return gridAgendamentos;
            if (tabs.SelectedTab == tabs.TabPages["Vendas"]) return gridVendas;
            return gridOrcamentos;
        }

        private void Carregar()
        {
            long cid = _cliente != null ? _cliente.Id : 0;

            gridOrcamentos.Columns.Clear();
            gridOrcamentos.Columns.Add("Numero", "Nº");
            gridOrcamentos.Columns.Add("Data", "Data");
            gridOrcamentos.Columns.Add("Servico", "Serviço");
            gridOrcamentos.Columns.Add("Status", "Status");
            gridOrcamentos.Columns.Add("Valor", "Valor (R$)");
            gridOrcamentos.Columns.Add("Tecnico", "Técnico");

            List<Orcamento> orcs = ServicoDAO.ListarOrcamentos().FindAll(o => o.ClienteId == cid);
            foreach (var o in orcs)
            {
                gridOrcamentos.Rows.Add(
                    string.IsNullOrWhiteSpace(o.Numero) ? o.Id.ToString("0000") : o.Numero,
                    o.Data,
                    o.Servico,
                    TraduzirStatus(o.Status),
                    o.Valor.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")),
                    o.NomeTecnico);
            }

            gridVendas.Columns.Clear();
            gridVendas.Columns.Add("Id", "Nº Venda");
            gridVendas.Columns.Add("Data", "Data");
            gridVendas.Columns.Add("Valor", "Valor (R$)");
            gridVendas.Columns.Add("Pagto", "Forma de pagamento");
            gridVendas.Columns.Add("Veiculo", "Veículo");

            List<Venda> vendas = VendaDAO.Listar(cid);
            foreach (var v in vendas)
            {
                gridVendas.Rows.Add(v.Id, v.Data,
                    v.ValorTotal.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")),
                    v.FormaPagamento, v.VeiculoDesc);
            }

            gridAgendamentos.Columns.Clear();
            gridAgendamentos.Columns.Add("Data", "Data / Hora");
            gridAgendamentos.Columns.Add("Servico", "Serviço");
            gridAgendamentos.Columns.Add("Status", "Status");
            gridAgendamentos.Columns.Add("Obs", "Observações");

            List<Agendamento> agendamentos = ServicoDAO.ListarAgendamentos().FindAll(a => a.ClienteId == cid);
            foreach (var a in agendamentos)
            {
                gridAgendamentos.Rows.Add(a.DataHora, a.NomeServico, TraduzirStatusAgendamento(a.Status), a.Observacoes);
            }
        }

        private static string TraduzirStatus(string s)
        {
            switch ((s ?? "").Trim().ToLower())
            {
                case "em_aberto": return "Em aberto";
                case "aprovado": return "Aprovado";
                case "recusado": return "Recusado";
                case "convertido": return "Convertido em venda";
                default: return s ?? "";
            }
        }

        private static string TraduzirStatusAgendamento(string s)
        {
            switch ((s ?? "").Trim().ToLower())
            {
                case "agendado": return "Agendado";
                case "confirmado": return "Confirmado";
                case "concluido": return "Concluído";
                case "cancelado": return "Cancelado";
                default: return s ?? "";
            }
        }
    }
}
