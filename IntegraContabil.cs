using System;
using System.Collections.Generic;
using System.Globalization;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Integração Contábil: exporta vendas, caixa e contas para arquivo CSV.</summary>
    public partial class IntegraContabil : Form
    {
        private ListBox lstModulos;
        private Button btnExportar;
        private Label lblStatus;

        public IntegraContabil()
        {
            InitializeComponent();
            Text = "Soen - Integração Contábil (Exportação)";
            ClientSize = new Size(520, 300);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
        }

        private void CriarInterface()
        {
            var l1 = new Label { Text = "Selecione o que exportar:", AutoSize = true, Location = new Point(12, 15) };
            lstModulos = new ListBox
            {
                Location = new Point(12, 40),
                Size = new Size(300, 160),
                SelectionMode = SelectionMode.MultiExtended
            };
            lstModulos.Items.Add("Vendas");
            lstModulos.Items.Add("Caixa (entradas e saídas)");
            lstModulos.Items.Add("Contas a Pagar");
            lstModulos.Items.Add("Contas a Receber");
            lstModulos.Items.Add("Clientes");
            lstModulos.Items.Add("Produtos");

            btnExportar = new Button { Text = "Exportar CSV", Location = new Point(330, 40), Size = new Size(130, 28), BackColor = SystemColors.AppWorkspace };
            btnExportar.Click += (s, e) => Exportar();

            lblStatus = new Label { Text = "", AutoSize = true, Location = new Point(12, 220) };

            Controls.AddRange(new Control[] { l1, lstModulos, btnExportar, lblStatus });
        }

        private void Exportar()
        {
            if (lstModulos.SelectedItems.Count == 0)
            {
                MessageBox.Show("Selecione ao menos um módulo.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "soen_contabil_" + DateTime.Now.ToString("yyyyMMdd") + ".csv" })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                var sb = new StringBuilder();
                if (lstModulos.SelectedItems.Contains("Vendas")) ExportarVendas(sb);
                if (lstModulos.SelectedItems.Contains("Caixa (entradas e saídas)")) ExportarCaixa(sb);
                if (lstModulos.SelectedItems.Contains("Contas a Pagar")) ExportarContas(sb, "pagar");
                if (lstModulos.SelectedItems.Contains("Contas a Receber")) ExportarContas(sb, "receber");
                if (lstModulos.SelectedItems.Contains("Clientes")) ExportarClientes(sb);
                if (lstModulos.SelectedItems.Contains("Produtos")) ExportarProdutos(sb);
                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                lblStatus.Text = "Exportado com sucesso!";
            }
        }

        private static void ExportarVendas(StringBuilder sb)
        {
            sb.AppendLine("VENDA_ID;DATA;CLIENTE;VALOR_TOTAL;FORMA_PAGAMENTO");
            foreach (Venda v in VendaDAO.Listar(null))
                sb.AppendLine(string.Join(";", v.Id, v.Data, Escape(v.NomeCliente), v.ValorTotal.ToString("F2", CultureInfo.InvariantCulture), Escape(v.FormaPagamento)));
        }

        private static void ExportarCaixa(StringBuilder sb)
        {
            sb.AppendLine("CAIXA_ID;DATA;TIPO;DESCRICAO;VALOR");
            foreach (LancamentoCaixa l in CaixaDAO.Listar())
                sb.AppendLine(string.Join(";", l.Id, l.Data, l.Tipo, Escape(l.Descricao), l.Valor.ToString("F2", CultureInfo.InvariantCulture)));
        }

        private static void ExportarContas(StringBuilder sb, string tipo)
        {
            sb.AppendLine("CONTA_ID;TIPO;DESCRICAO;FORNECEDOR;VENCIMENTO;VALOR;STATUS");
            foreach (ContaFinanceira c in FinanceiroDAO.Listar(tipo))
                sb.AppendLine(string.Join(";", c.Id, c.Tipo, Escape(c.Descricao), Escape(c.Fornecedor), c.Vencimento, c.Valor.ToString("F2", CultureInfo.InvariantCulture), c.Status));
        }

        private static void ExportarClientes(StringBuilder sb)
        {
            sb.AppendLine("CLIENTE_ID;TIPO;NOME;CPF_CNPJ;CIDADE;FONE;EMAIL");
            foreach (Cliente c in ClienteDAO.Listar(""))
                sb.AppendLine(string.Join(";", c.Id, c.Tipo, Escape(c.NomeRazao), Escape(c.CpfCnpj), Escape(c.Cidade), Escape(c.Fone1), Escape(c.Email1)));
        }

        private static void ExportarProdutos(StringBuilder sb)
        {
            sb.AppendLine("PRODUTO_ID;CODIGO;NOME;CATEGORIA;QTD;CUSTO;PRECO");
            foreach (Produto p in ProdutoDAO.Listar(""))
                sb.AppendLine(string.Join(";", p.Id, Escape(p.Codigo), Escape(p.Nome), Escape(p.Categoria), p.QtdAtual.ToString("F2", CultureInfo.InvariantCulture), p.Custo.ToString("F2", CultureInfo.InvariantCulture), p.Preco.ToString("F2", CultureInfo.InvariantCulture)));
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }
    }
}