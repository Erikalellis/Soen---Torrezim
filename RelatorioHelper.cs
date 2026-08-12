using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Soen___Torrezim.Common;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Exportação CSV e impressão de DataGridView (relatórios).</summary>
    public static class RelatorioHelper
    {
        // ===== Impressão formatada de Orçamento / Nota de Serviço (OS) =====

        public static void VisualizarDocumento(Orcamento o, Cliente c, Veiculo v)
        {
            if (o == null) return;
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            bool nota = o.Tipo == "nota";
            Empresa emp = EmpresaDAO.Obter();
            string nomeEmpresa = string.IsNullOrWhiteSpace(emp.Nome) ? "Soen - Sistema de Gestão" : emp.Nome;

            var linhas = new List<DocLine>();
            linhas.Add(new DocLine(nomeEmpresa, DocStyle.Titulo));
            if (!string.IsNullOrWhiteSpace(emp.Endereco) || !string.IsNullOrWhiteSpace(emp.Telefone))
                linhas.Add(new DocLine(ContatoEmpresa(emp), DocStyle.Normal));
            linhas.Add(new DocLine(nota ? "NOTA DE SERVIÇO / ORDEM DE SERVIÇO" : "ORÇAMENTO", DocStyle.Subtitulo));
            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("Nº " + (string.IsNullOrWhiteSpace(o.Numero) ? (nota ? "OS-" + o.Id.ToString("0000") : o.Id.ToString("0000")) : o.Numero)
                + "        Emitido em: " + o.Data, DocStyle.Normal));

            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("DADOS DO CLIENTE", DocStyle.Secao));
            linhas.Add(new DocLine("Cliente: " + (c != null ? c.NomeRazao : "") , DocStyle.Normal));
            if (c != null && !string.IsNullOrWhiteSpace(c.CpfCnpj))
                linhas.Add(new DocLine("CPF/CNPJ: " + c.CpfCnpj + (string.IsNullOrWhiteSpace(c.Fone1) ? "" : "      Telefone: " + c.Fone1), DocStyle.Normal));
            if (c != null && !string.IsNullOrWhiteSpace(c.Endereco))
                linhas.Add(new DocLine("Endereço: " + c.Endereco + (string.IsNullOrWhiteSpace(c.Cidade) ? "" : " - " + c.Cidade + "/" + c.Estado), DocStyle.Normal));

            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("VEÍCULO", DocStyle.Secao));
            if (v != null)
                linhas.Add(new DocLine("Placa: " + v.Placa + "        " + v.Marca + " " + v.Modelo + (string.IsNullOrWhiteSpace(v.Cor) ? "" : " - " + v.Cor), DocStyle.Normal));
            else
                linhas.Add(new DocLine("(veículo não informado)", DocStyle.Normal));

            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("SERVIÇO(S)", DocStyle.Secao));
            if (o.Itens != null && o.Itens.Count > 0)
            {
                int n = 0;
                foreach (OrcamentoItem it in o.Itens)
                {
                    n++;
                    string linhaItem = "#" + n + "  " + it.Descricao;
                    if (it.Quantidade > 1)
                        linhaItem += "   (x" + it.Quantidade.ToString("0.##") + ")";
                    linhas.Add(new DocLine(linhaItem + "   = " + it.ValorTotal.ToString("N2", cult), DocStyle.Normal));
                }
            }
            else
            {
                linhas.Add(new DocLine("• " + o.Servico, DocStyle.Normal));
            }
            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("Valor total:  " + o.Valor.ToString("N2", cult), DocStyle.Destaque));

            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("Situação: " + TraduzirStatus(o.Status), DocStyle.Normal));
            if (!string.IsNullOrWhiteSpace(o.NomeTecnico) || o.Comissao > 0)
            {
                linhas.Add(new DocLine("Técnico responsável: " + (string.IsNullOrWhiteSpace(o.NomeTecnico) ? "—" : o.NomeTecnico), DocStyle.Normal));
                if (o.Comissao > 0)
                    linhas.Add(new DocLine("Comissão: R$ " + o.Comissao.ToString("N2", cult), DocStyle.Normal));
            }

            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine(nota
                ? "Documento de serviço executado. Garantia de 90 dias sobre o serviço."
                : "Este orçamento tem validade de 30 dias e NÃO gera compromisso.", DocStyle.Normal));
            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("_______________________________________________", DocStyle.Normal));
            linhas.Add(new DocLine("Cliente / Responsável", DocStyle.Normal));
            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("Atenciosamente,", DocStyle.Normal));
            linhas.Add(new DocLine(nomeEmpresa, DocStyle.Normal));
            if (!string.IsNullOrWhiteSpace(emp.Email) || !string.IsNullOrWhiteSpace(emp.Site))
                linhas.Add(new DocLine(ContatoRodape(emp), DocStyle.Normal));

            using (var dlg = new PrintDialog())
            using (var doc = new PrintDocument())
            {
                AplicarImpressoraPadrao(doc);
                ConfigurarA4(doc);
                var render = new DocPrinter(linhas, CarregarLogo(emp));
                doc.PrintPage += render.ImprimirPagina;
                using (var prev = new PrintPreviewDialog { Document = doc, Width = 700, Height = 800 })
                {
                    prev.ShowDialog();
                }
                render.ReleaseLogo();
                ArquivarDocumentoPdf(linhas, nota ? "os" : "orcamento",
                    string.IsNullOrWhiteSpace(o.Numero) ? o.Id.ToString("0000") : o.Numero);
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

        /// <summary>Recibo de um lançamento de caixa (entrada/saída).</summary>
        public static void VisualizarReciboCaixa(Soen___Torrezim.Models.LancamentoCaixa l)
        {
            if (l == null) return;
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            Empresa emp = EmpresaDAO.Obter();
            string nomeEmpresa = string.IsNullOrWhiteSpace(emp.Nome) ? "Soen - Sistema de Gestão" : emp.Nome;

            bool entrada = l.Tipo == "entrada";

            var linhas = new List<DocLine>();
            linhas.Add(new DocLine(nomeEmpresa, DocStyle.Titulo));
            if (!string.IsNullOrWhiteSpace(emp.Endereco) || !string.IsNullOrWhiteSpace(emp.Telefone))
                linhas.Add(new DocLine(ContatoEmpresa(emp), DocStyle.Normal));
            linhas.Add(new DocLine("RECIBO DE CAIXA", DocStyle.Subtitulo));
            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("Nº " + l.Id + "        Data: " + l.Data, DocStyle.Normal));
            linhas.Add(new DocLine("Tipo: " + (entrada ? "ENTRADA" : "SAÍDA"), DocStyle.Normal));
            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("Descrição: " + l.Descricao, DocStyle.Normal));
            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine((entrada ? "VALOR RECEBIDO: " : "VALOR PAGO: ") + "R$ " + l.Valor.ToString("N2", cult), DocStyle.Destaque));
            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("_______________________________________________", DocStyle.Normal));
            linhas.Add(new DocLine(nomeEmpresa, DocStyle.Normal));
            if (!string.IsNullOrWhiteSpace(emp.Email) || !string.IsNullOrWhiteSpace(emp.Site))
                linhas.Add(new DocLine(ContatoRodape(emp), DocStyle.Normal));

            using (var dlg = new PrintDialog())
            using (var doc = new PrintDocument())
            {
                AplicarImpressoraPadrao(doc);
                ConfigurarA4(doc);
                var render = new DocPrinter(linhas, CarregarLogo(emp));
                doc.PrintPage += render.ImprimirPagina;
                using (var prev = new PrintPreviewDialog { Document = doc, Width = 700, Height = 800 })
                {
                    prev.ShowDialog();
                }
                render.ReleaseLogo();
                ArquivarDocumentoPdf(linhas, "caixa", l.Id.ToString());
            }
        }

        /// <summary>Fechamento/resumo diário do caixa: lista os movimentos do dia,
        /// totais de entradas/saídas, saldo do dia e saldo acumulado.</summary>
        public static void VisualizarResumoCaixa(DateTime dia, System.Collections.Generic.List<Models.LancamentoCaixa> movs)
        {
            if (movs == null) return;
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            Empresa emp = EmpresaDAO.Obter();
            string nomeEmpresa = string.IsNullOrWhiteSpace(emp.Nome) ? "Soen - Sistema de Gestão" : emp.Nome;

            double entradas = 0, saidas = 0;
            foreach (var l in movs)
            {
                if (l.Tipo == "entrada") entradas += l.Valor; else saidas += l.Valor;
            }
            double saldoDia = entradas - saidas;
            double saldoGeral = CaixaDAO.Saldo();

            var linhas = new List<DocLine>();
            linhas.Add(new DocLine(nomeEmpresa, DocStyle.Titulo));
            if (!string.IsNullOrWhiteSpace(emp.Endereco) || !string.IsNullOrWhiteSpace(emp.Telefone))
                linhas.Add(new DocLine(ContatoEmpresa(emp), DocStyle.Normal));
            linhas.Add(new DocLine("FECHAMENTO DO CAIXA", DocStyle.Subtitulo));
            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("Dia: " + dia.ToString("dddd, dd/MM/yyyy", cult), DocStyle.Normal));
            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("MOVIMENTAÇÕES", DocStyle.Secao));
            if (movs.Count == 0)
                linhas.Add(new DocLine("Nenhum lançamento neste dia.", DocStyle.Normal));
            foreach (var l in movs)
            {
                string tipo = l.Tipo == "entrada" ? "ENTRADA" : "SAÍDA";
                linhas.Add(new DocLine(l.Data + "   " + tipo + "   " + l.Descricao +
                    "   = R$ " + l.Valor.ToString("N2", cult), DocStyle.Normal));
            }

            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("Entradas do dia:  R$ " + entradas.ToString("N2", cult), DocStyle.Normal));
            linhas.Add(new DocLine("Saídas do dia:    R$ " + saidas.ToString("N2", cult), DocStyle.Normal));
            linhas.Add(new DocLine("Saldo do dia:     R$ " + saldoDia.ToString("N2", cult), DocStyle.Destaque));
            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("Saldo acumulado do caixa:  R$ " + saldoGeral.ToString("N2", cult), DocStyle.Destaque));

            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("_______________________________________________", DocStyle.Normal));
            linhas.Add(new DocLine(nomeEmpresa, DocStyle.Normal));
            if (!string.IsNullOrWhiteSpace(emp.Email) || !string.IsNullOrWhiteSpace(emp.Site))
                linhas.Add(new DocLine(ContatoRodape(emp), DocStyle.Normal));

            using (var dlg = new PrintDialog())
            using (var doc = new PrintDocument())
            {
                AplicarImpressoraPadrao(doc);
                ConfigurarA4(doc);
                var render = new DocPrinter(linhas, CarregarLogo(emp));
                doc.PrintPage += render.ImprimirPagina;
                using (var prev = new PrintPreviewDialog { Document = doc, Width = 700, Height = 800 })
                {
                    prev.ShowDialog();
                }
                render.ReleaseLogo();
                ArquivarDocumentoPdf(linhas, "fechamento_caixa", dia.ToString("yyyyMMdd"));
            }
        }

        private static string ContatoEmpresa(Empresa emp)
        {
            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(emp.Endereco)) partes.Add(emp.Endereco);
            if (!string.IsNullOrWhiteSpace(emp.Telefone)) partes.Add("Tel: " + emp.Telefone);
            if (!string.IsNullOrWhiteSpace(emp.Email)) partes.Add(emp.Email);
            return string.Join("   |   ", partes);
        }

        private static string ContatoRodape(Empresa emp)
        {
            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(emp.Email)) partes.Add(emp.Email);
            if (!string.IsNullOrWhiteSpace(emp.Site)) partes.Add(emp.Site);
            return string.Join("   ", partes);
        }

        /// <summary>Recibo/nota de venda: identifica cliente, lista os itens e o total.</summary>
        public static void VisualizarReciboVenda(Soen___Torrezim.Models.Venda v, Soen___Torrezim.Models.Cliente c)
        {
            if (v == null) return;
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            Empresa emp = EmpresaDAO.Obter();
            string nomeEmpresa = string.IsNullOrWhiteSpace(emp.Nome) ? "Soen - Sistema de Gestão" : emp.Nome;

            var linhas = new List<DocLine>();
            linhas.Add(new DocLine(nomeEmpresa, DocStyle.Titulo));
            if (!string.IsNullOrWhiteSpace(emp.Endereco) || !string.IsNullOrWhiteSpace(emp.Telefone))
                linhas.Add(new DocLine(ContatoEmpresa(emp), DocStyle.Normal));
            linhas.Add(new DocLine("RECIBO DE VENDA", DocStyle.Subtitulo));
            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("Nº " + v.Id + "        Emitido em: " + v.Data, DocStyle.Normal));
            linhas.Add(new DocLine("", DocStyle.Normal));

            linhas.Add(new DocLine("CLIENTE", DocStyle.Secao));
            linhas.Add(new DocLine("Cliente: " + (c != null ? c.NomeRazao : ""), DocStyle.Normal));
            if (c != null && !string.IsNullOrWhiteSpace(c.CpfCnpj))
                linhas.Add(new DocLine("CPF/CNPJ: " + c.CpfCnpj, DocStyle.Normal));
            if (!string.IsNullOrWhiteSpace(v.VeiculoDesc))
                linhas.Add(new DocLine("Veículo: " + v.VeiculoDesc, DocStyle.Normal));

            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("ITENS", DocStyle.Secao));
            if (v.Itens != null && v.Itens.Count > 0)
            {
                int n = 0;
                foreach (var it in v.Itens)
                {
                    n++;
                    string linhaItem = "#" + n + "  " + it.Descricao;
                    if (it.Quantidade > 1)
                        linhaItem += "   (x" + it.Quantidade.ToString("0.##") + ")";
                    linhas.Add(new DocLine(linhaItem + "   = " + ((double)(it.Quantidade * it.ValorUnit)).ToString("N2", cult), DocStyle.Normal));
                }
            }
            else
            {
                linhas.Add(new DocLine("(sem itens detalhados)", DocStyle.Normal));
            }
            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("TOTAL: R$ " + v.ValorTotal.ToString("N2", cult), DocStyle.Destaque));
            if (!string.IsNullOrWhiteSpace(v.FormaPagamento))
                linhas.Add(new DocLine("Forma de pagamento: " + v.FormaPagamento, DocStyle.Normal));

            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine("_______________________________________________", DocStyle.Normal));
            linhas.Add(new DocLine("Recebi conforme descrito acima.", DocStyle.Normal));
            linhas.Add(new DocLine("", DocStyle.Normal));
            linhas.Add(new DocLine(nomeEmpresa, DocStyle.Normal));
            if (!string.IsNullOrWhiteSpace(emp.Email) || !string.IsNullOrWhiteSpace(emp.Site))
                linhas.Add(new DocLine(ContatoRodape(emp), DocStyle.Normal));

            using (var dlg = new PrintDialog())
            using (var doc = new PrintDocument())
            {
                AplicarImpressoraPadrao(doc);
                ConfigurarA4(doc);
                var render = new DocPrinter(linhas, CarregarLogo(emp));
                doc.PrintPage += render.ImprimirPagina;
                using (var prev = new PrintPreviewDialog { Document = doc, Width = 700, Height = 800 })
                {
                    prev.ShowDialog();
                }
                render.ReleaseLogo();
                ArquivarDocumentoPdf(linhas, "venda", v.Id.ToString());
            }
        }

        /// <summary>
        /// Arquiva o documento (recibo de venda/caixa, orçamento/OS) como PDF na pasta
        /// do aplicativo (recibos\AAAA\MM\). Assim os documentos ficam salvos mesmo que
        /// o banco venha a falhar. Falhas aqui não interrompem a impressão.
        /// </summary>
        private static void ArquivarDocumentoPdf(List<DocLine> linhas, string prefixo, string numero)
        {
            if (linhas == null || linhas.Count == 0) return;
            try
            {
                string pasta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recibos",
                    DateTime.Now.ToString("yyyy"), DateTime.Now.ToString("MM"));
                Directory.CreateDirectory(pasta);

                string seguro = (numero ?? "").Replace("/", "-").Replace(":", "-");
                string nome = (prefixo + "_" + seguro + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf").Replace(" ", "_");
                string caminho = Path.Combine(pasta, nome);

                var bmp = RenderDocParaBitmap(linhas);
                try
                {
                    Common.Exportacao.SalvarPdf(caminho, new List<Bitmap> { bmp });
                }
                finally
                {
                    bmp.Dispose();
                }
            }
            catch { /* arquivamento é opcional */ }
        }

        /// <summary>Desenha as linhas de um documento em uma página A4 (para gerar o PDF).</summary>
        private static Bitmap RenderDocParaBitmap(List<DocLine> linhas)
        {
            const int A4W = 794;
            const int A4H = 1123;
            var bmp = new Bitmap(A4W, A4H);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                float y = 60f;
                foreach (DocLine l in linhas)
                {
                    Font fonte;
                    switch (l.Style)
                    {
                        case DocStyle.Titulo: fonte = new Font("Arial", 15, FontStyle.Bold); break;
                        case DocStyle.Subtitulo: fonte = new Font("Arial", 12, FontStyle.Bold); break;
                        case DocStyle.Secao: fonte = new Font("Arial", 10, FontStyle.Bold); break;
                        case DocStyle.Destaque: fonte = new Font("Arial", 11, FontStyle.Bold); break;
                        default: fonte = new Font("Arial", 10); break;
                    }
                    using (fonte)
                    {
                        float h = g.MeasureString(l.Text, fonte).Height;
                        if (l.Style == DocStyle.Titulo || l.Style == DocStyle.Subtitulo)
                        {
                            var sf = new StringFormat { Alignment = StringAlignment.Center };
                            g.DrawString(l.Text, fonte, Brushes.Black, new RectangleF(60, y, A4W - 120, h), sf);
                        }
                        else
                        {
                            g.DrawString(l.Text, fonte, Brushes.Black, 60, y);
                        }
                        y += h + (l.Style == DocStyle.Secao ? 4f : 2f);
                    }
                }
            }
            return bmp;
        }

        private class DocLine
        {
            public string Text;
            public DocStyle Style;
            public DocLine(string t, DocStyle s) { Text = t; Style = s; }
        }

        private enum DocStyle { Titulo, Subtitulo, Secao, Normal, Destaque }

        /// <summary>Renderiza linhas formatadas de um documento no PrintDocument.</summary>
        private class DocPrinter
        {
            private readonly List<DocLine> _linhas;
            private readonly Image _logo;
            private readonly float _logoExtra;
            private int _linha = 0;
            private bool _primeiraPagina = true;

            public DocPrinter(List<DocLine> linhas, Image logo)
            {
                _linhas = linhas;
                _logo = logo;
                _logoExtra = logo != null ? 74f : 0f;
            }

            /// <summary>Libera a imagem do logo após a visualização/impressão.</summary>
            public void ReleaseLogo()
            {
                if (_logo != null) _logo.Dispose();
            }

            public void ImprimirPagina(object sender, PrintPageEventArgs e)
            {
                bool usaLogo = _logo != null && _primeiraPagina;
                if (usaLogo)
                {
                    try { e.Graphics.DrawImage(_logo, e.MarginBounds.Left, e.MarginBounds.Top, 100f, 64f); }
                    catch (Exception ex) { Logger.LogError(ex); }
                }
                _primeiraPagina = false;

                float y = e.MarginBounds.Top + (usaLogo ? _logoExtra : 0f);
                float maxY = e.MarginBounds.Bottom;
                float largura = e.MarginBounds.Width;

                bool mais = false;
                while (_linha < _linhas.Count)
                {
                    DocLine l = _linhas[_linha];
                    Font fonte;
                    switch (l.Style)
                    {
                        case DocStyle.Titulo: fonte = new Font("Arial", 15, FontStyle.Bold); break;
                        case DocStyle.Subtitulo: fonte = new Font("Arial", 12, FontStyle.Bold); break;
                        case DocStyle.Secao: fonte = new Font("Arial", 10, FontStyle.Bold); break;
                        case DocStyle.Destaque: fonte = new Font("Arial", 11, FontStyle.Bold); break;
                        default: fonte = new Font("Arial", 10); break;
                    }
                    using (fonte)
                    {
                        float h = e.Graphics.MeasureString(l.Text, fonte).Height;
                        if (y + h > maxY)
                        {
                            mais = true;
                            break;
                        }
                        var al = l.Style == DocStyle.Titulo || l.Style == DocStyle.Subtitulo
                            ? StringFormat.GenericDefault
                            : new StringFormat { Alignment = StringAlignment.Near };
                        if (l.Style == DocStyle.Titulo || l.Style == DocStyle.Subtitulo)
                        {
                            var sf = new StringFormat { Alignment = StringAlignment.Center };
                            e.Graphics.DrawString(l.Text, fonte, Brushes.Black,
                                new RectangleF(e.MarginBounds.Left, y, largura, h), sf);
                        }
                        else
                        {
                            e.Graphics.DrawString(l.Text, fonte, Brushes.Black, e.MarginBounds.Left, y);
                        }
                        y += h + (l.Style == DocStyle.Secao ? 4f : 2f);
                    }
                    _linha++;
                }
                e.HasMorePages = mais;
            }
        }
        public static void ExportarCsv(DataGridView grid, string nomeSugerido)
        {
            if (grid.Columns.Count == 0)
            {
                MessageBox.Show("Não há dados para exportar.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "CSV (*.csv)|*.csv";
                dlg.FileName = nomeSugerido;
                dlg.DefaultExt = "csv";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                var l = new StringBuilder();
                for (int c = 0; c < grid.Columns.Count; c++)
                {
                    if (c > 0) l.Append(';');
                    l.Append(Escape(grid.Columns[c].HeaderText));
                }
                sb.AppendLine(l.ToString());

                foreach (DataGridViewRow r in grid.Rows)
                {
                    if (r.IsNewRow) continue;
                    l = new StringBuilder();
                    for (int c = 0; c < grid.Columns.Count; c++)
                    {
                        if (c > 0) l.Append(';');
                        object v = r.Cells[c].Value;
                        l.Append(Escape(v == null ? "" : v.ToString()));
                    }
                    sb.AppendLine(l.ToString());
                }

                try
                {
                    File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Relatório exportado com sucesso!", "SOEN", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                    MessageBox.Show("Erro ao exportar. Veja o log para detalhes.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public static void ExportarExcel(DataGridView grid, string nomeSugerido)
        {
            if (grid.Columns.Count == 0)
            {
                MessageBox.Show("Não há dados para exportar.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "Excel (*.xlsx)|*.xlsx";
                dlg.FileName = nomeSugerido;
                dlg.DefaultExt = "xlsx";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    Common.Exportacao.SalvarXlsx(grid, dlg.FileName);
                    MessageBox.Show("Planilha exportada com sucesso!", "SOEN", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                    MessageBox.Show("Erro ao exportar Excel. Veja o log para detalhes.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public static void ExportarPdf(DataGridView grid, string titulo, string nomeSugerido)
        {
            if (grid.Columns.Count == 0)
            {
                MessageBox.Show("Não há dados para exportar.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "PDF (*.pdf)|*.pdf";
                dlg.FileName = nomeSugerido;
                dlg.DefaultExt = "pdf";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    Common.Exportacao.SalvarPdfGrid(grid, titulo, dlg.FileName);
                    MessageBox.Show("PDF gerado com sucesso!", "SOEN", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                    MessageBox.Show("Erro ao gerar o PDF. Veja o log para detalhes.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public static void AdicionarBotoesExportar(Form form, Func<DataGridView> obterGrid,
            string titulo, string baseArquivo, int x, int y)
        {
            var btnPdf = UIHelpers.CreateButton("PDF", new Point(x, y), new Size(66, 28));
            btnPdf.Click += (s, e) => ExportarPdf(obterGrid(), titulo, baseArquivo + ".pdf");
            var btnExcel = UIHelpers.CreateButton("Excel", new Point(x + 74, y), new Size(72, 28));
            btnExcel.Click += (s, e) => ExportarExcel(obterGrid(), baseArquivo + ".xlsx");
            form.Controls.Add(btnPdf);
            form.Controls.Add(btnExcel);
        }

        public static void Imprimir(DataGridView grid, string titulo)
        {
            if (grid.Columns.Count == 0)
            {
                MessageBox.Show("Não há dados para imprimir.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var pd = new PrintDialog())
            using (var doc = new PrintDocument())
            {
                AplicarImpressoraPadrao(doc);
                if (pd.ShowDialog() != DialogResult.OK) return;
                var render = new GridPrinter(grid, titulo);
                doc.PrintPage += render.ImprimirPagina;
                try { doc.Print(); }
                catch (Exception ex)
                {
                    Logger.LogError(ex);
                    MessageBox.Show("Erro ao imprimir. Veja o log para detalhes.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static string Escape(string s)
        {
            if (s == null) return "";
            if (s.Contains(";") || s.Contains("\"") || s.Contains("\n"))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        /// <summary>Configura o documento para folha A4.</summary>
        private static void ConfigurarA4(PrintDocument doc)
        {
            try
            {
                doc.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("A4", 827, 1169);
            }
            catch (Exception ex) { Logger.LogError(ex); }
        }

        /// <summary>Carrega a imagem da logo da empresa (ou null se não configurada).</summary>
        private static Image CarregarLogo(Empresa emp)
        {
            try
            {
                if (emp != null && !string.IsNullOrWhiteSpace(emp.LogoPath) && System.IO.File.Exists(emp.LogoPath))
                {
                    using (var img = Image.FromFile(emp.LogoPath))
                        return new Bitmap(img);
                }
            }
            catch (Exception ex) { Logger.LogError(ex); }
            return null;
        }

        /// <summary>Aplica a impressora padrão salva nas configurações ao documento.</summary>
        private static void AplicarImpressoraPadrao(PrintDocument doc)
        {
            try
            {
                string nome = ImpressoraConfig.Padrao;
                if (string.IsNullOrWhiteSpace(nome)) return;
                foreach (string p in PrinterSettings.InstalledPrinters)
                {
                    if (string.Equals(p, nome, StringComparison.OrdinalIgnoreCase))
                    {
                        doc.PrinterSettings.PrinterName = p;
                        return;
                    }
                }
            }
            catch
            {
                // mantém a impressora padrão do sistema em caso de problema
            }
        }

        /// <summary>Desenha o conteúdo de um DataGridView no PrintDocument, com paginação.</summary>
        private class GridPrinter
        {
            private readonly DataGridView _grid;
            private readonly string _titulo;
            private int _linha = 0;

            public GridPrinter(DataGridView grid, string titulo)
            {
                _grid = grid;
                _titulo = titulo;
            }

            public void ImprimirPagina(object sender, PrintPageEventArgs e)
            {
                float margem = e.MarginBounds.Left;
                float largura = e.MarginBounds.Width;
                float y = e.MarginBounds.Top;
                float alturaLinha = 24f;

                using (var fTitulo = new Font("Arial", 12, FontStyle.Bold))
                using (var fCab = new Font("Arial", 9, FontStyle.Bold))
                using (var f = new Font("Arial", 9))
                {
                    // desenha logo (se configurado)
                    float logoOffsetX = 0f;
                    try
                    {
                        var cfg = Data.EmpresaDAO.Obter();
                        if (!string.IsNullOrWhiteSpace(cfg.LogoPath) && System.IO.File.Exists(cfg.LogoPath))
                        {
                            using (var img = Image.FromFile(cfg.LogoPath))
                            {
                                int lw = cfg.LogoWidth > 0 ? cfg.LogoWidth : 80;
                                int lh = cfg.LogoHeight > 0 ? cfg.LogoHeight : (int)(80.0 * img.Height / img.Width);
                                e.Graphics.DrawImage(img, margem, y, lw, lh);
                                logoOffsetX = lw + 10f;
                            }
                        }
                    }
                    catch (Exception ex) { Logger.LogError(ex); }

                    e.Graphics.DrawString(_titulo, fTitulo, Brushes.Black, margem + logoOffsetX, y);
                    y += 30f;

                    float cw = largura / Math.Max(1, _grid.Columns.Count);
                    for (int c = 0; c < _grid.Columns.Count; c++)
                        e.Graphics.DrawString(_grid.Columns[c].HeaderText, fCab, Brushes.Black, margem + c * cw, y);
                    y += alturaLinha;
                    e.Graphics.DrawLine(Pens.Black, margem, y, margem + largura, y);
                    y += 6f;

                    bool maisPagina = false;
                    while (_linha < _grid.Rows.Count)
                    {
                        DataGridViewRow r = _grid.Rows[_linha];
                        _linha++;
                        if (r.IsNewRow) continue;
                        if (y + alturaLinha > e.MarginBounds.Bottom)
                        {
                            maisPagina = true;
                            break;
                        }
                        for (int c = 0; c < _grid.Columns.Count; c++)
                        {
                            object v = r.Cells[c].Value;
                            e.Graphics.DrawString(v == null ? "" : v.ToString(), f, Brushes.Black, margem + c * cw, y);
                        }
                        y += alturaLinha;
                    }
                    e.HasMorePages = maisPagina;
                }
            }
        }
    }
}