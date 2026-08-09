using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Forms;

namespace Soen___Torrezim.Common
{
    /// <summary>
    /// Exportação direta de relatórios em arquivos reais .pdf e .xlsx,
    /// sem dependência de bibliotecas externas.
    /// </summary>
    public static class Exportacao
    {
        // =====================================================================
        // XLSX (planilha Excel)
        // =====================================================================

        public static void SalvarXlsx(DataGridView grid, string caminho)
        {
            var sheet = new StringBuilder();
            sheet.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sheet.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            sheet.Append("<sheetData>");
            sheet.Append("<row r=\"1\">");
            for (int c = 0; c < grid.Columns.Count; c++)
                sheet.Append(Celula(c + 1, grid.Columns[c].HeaderText, true));
            sheet.Append("</row>");

            int r = 2;
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                sheet.Append("<row r=\"" + r + "\">");
                for (int c = 0; c < grid.Columns.Count; c++)
                {
                    object v = row.Cells[c].Value;
                    sheet.Append(Celula(c + 1, v == null ? "" : v.ToString(), false));
                }
                sheet.Append("</row>");
                r++;
            }
            sheet.Append("</sheetData></worksheet>");

            const string ct =
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                "</Types>";

            const string rels =
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>";

            const string workbook =
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheets><sheet name=\"Relatorio\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";

            const string wbRels =
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                "</Relationships>";

            using (var fs = File.Create(caminho))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                AdicionarZip(zip, "[Content_Types].xml", ct);
                AdicionarZip(zip, "_rels/.rels", rels);
                AdicionarZip(zip, "xl/workbook.xml", workbook);
                AdicionarZip(zip, "xl/_rels/workbook.xml.rels", wbRels);
                AdicionarZip(zip, "xl/worksheets/sheet1.xml", sheet.ToString());
            }
        }

        private static void AdicionarZip(ZipArchive zip, string nome, string conteudo)
        {
            var entry = zip.CreateEntry(nome, CompressionLevel.Optimal);
            using (var s = entry.Open())
            {
                var bytes = Encoding.UTF8.GetBytes(conteudo);
                s.Write(bytes, 0, bytes.Length);
            }
        }

        private static string Celula(int coluna, string valor, bool negrito)
        {
            string refe = ReferenciaCelula(coluna);
            string estilo = negrito ? " s=\"1\"" : "";
            double num;
            if (!negrito && double.TryParse(valor.Replace("R$", "").Replace(".", "").Replace(",", "."),
                NumberStyles.Any, CultureInfo.InvariantCulture, out num))
            {
                return "<c r=\"" + refe + "\"" + estilo + "><v>" +
                    num.ToString("0.##", CultureInfo.InvariantCulture) + "</v></c>";
            }
            return "<c r=\"" + refe + "\" t=\"inlineStr\"" + estilo + "><is><t xml:space=\"preserve\">" +
                EscXml(valor) + "</t></is></c>";
        }

        private static string ReferenciaCelula(int coluna)
        {
            string letra = "";
            int n = coluna;
            while (n > 0)
            {
                int resto = (n - 1) % 26;
                letra = (char)('A' + resto) + letra;
                n = (n - 1) / 26;
            }
            return letra;
        }

        private static string EscXml(string s)
        {
            if (s == null) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        // =====================================================================
        // PDF
        // =====================================================================

        private const int A4W = 794;   // 210mm a 96dpi
        private const int A4H = 1123;  // 297mm a 96dpi
        private const int Margem = 44;

        /// <summary>Renderiza o grid em páginas A4 (bitmap) e salva um .pdf real.</summary>
        public static void SalvarPdfGrid(DataGridView grid, string titulo, string caminho)
        {
            var paginas = RenderGridPaginas(grid, titulo);
            SalvarPdf(caminho, paginas);
            foreach (var b in paginas) b.Dispose();
        }

        private static List<Bitmap> RenderGridPaginas(DataGridView grid, string titulo)
        {
            var paginas = new List<Bitmap>();
            if (grid == null || grid.Columns.Count == 0) return paginas;

            float alturaLinha = 24f;
            float areaUtil = A4H - Margem * 2 - 30f - 30f;
            int linhasPorPagina = (int)Math.Floor(areaUtil / alturaLinha);

            int colCount = grid.Columns.Count;
            float largura = A4W - Margem * 2;
            float cw = largura / Math.Max(1, colCount);

            var fTitulo = new Font("Segoe UI", 14, FontStyle.Bold);
            var fCab = new Font("Segoe UI", 9, FontStyle.Bold);
            var f = new Font("Segoe UI", 9);

            using (fTitulo) using (fCab) using (f)
            {
                int linha = 0;
                while (linha < grid.Rows.Count)
                {
                    var bmp = new Bitmap(A4W, A4H);
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.White);
                        float y = Margem;
                        g.DrawString(titulo, fTitulo, Brushes.Black, Margem, y);
                        y += 30f;

                        // cabeçalho
                        for (int c = 0; c < colCount; c++)
                            g.DrawString(grid.Columns[c].HeaderText, fCab, Brushes.Black, Margem + c * cw, y);
                        y += alturaLinha;
                        g.DrawLine(Pens.Black, Margem, y, Margem + largura, y);
                        y += 6f;

                        int naPagina = 0;
                        while (linha < grid.Rows.Count && naPagina < linhasPorPagina)
                        {
                            var row = grid.Rows[linha];
                            linha++;
                            if (row.IsNewRow) continue;
                            for (int c = 0; c < colCount; c++)
                            {
                                object v = row.Cells[c].Value;
                                g.DrawString(v == null ? "" : v.ToString(), f, Brushes.Black, Margem + c * cw, y);
                            }
                            y += alturaLinha;
                            naPagina++;
                        }
                    }
                    paginas.Add(bmp);
                    if (linha >= grid.Rows.Count) break;
                }
            }
            if (paginas.Count == 0)
            {
                var vazio = new Bitmap(A4W, A4H);
                using (var g = Graphics.FromImage(vazio))
                {
                    g.Clear(Color.White);
                    g.DrawString(titulo, fTitulo, Brushes.Black, Margem, Margem);
                    g.DrawString("(sem dados para exibir)", f, Brushes.Black, Margem, Margem + 40);
                }
                paginas.Add(vazio);
            }
            return paginas;
        }

        /// <summary>Gera um arquivo PDF com uma página por bitmap (JPEG embutido).</summary>
        public static void SalvarPdf(string caminho, List<Bitmap> paginas)
        {
            using (var ms = new MemoryStream())
            {
                var offsets = new List<long>();
                int numObj = 2 + paginas.Count * 3;

                EscreverBytes(ms, Encoding.ASCII.GetBytes("%PDF-1.4\n%\u00e2\u00e3\u00cf\u00d2\n"));

                // objeto 1: catálogo
                offsets.Add(Posicao(ms));
                EscreverBytes(ms, Encoding.ASCII.GetBytes("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"));

                // objeto 2: páginas
                offsets.Add(Posicao(ms));
                var kids = new StringBuilder();
                for (int i = 0; i < paginas.Count; i++) kids.Append(" " + (3 + i * 3) + " 0 R");
                EscreverBytes(ms, Encoding.ASCII.GetBytes("2 0 obj\n<< /Type /Pages /Kids [" + kids + " ] /Count " + paginas.Count + " >>\nendobj\n"));

                // objetos de cada página: página, conteúdo, imagem
                for (int i = 0; i < paginas.Count; i++)
                {
                    int numPagina = 3 + i * 3;
                    int numConteudo = numPagina + 1;
                    int numImagem = numPagina + 2;

                    offsets.Add(Posicao(ms));
                    EscreverBytes(ms, Encoding.ASCII.GetBytes(numPagina + " 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595.28 841.89] " +
                        "/Resources << /XObject << /Im0 " + numImagem + " 0 R >> /ProcSet [/PDF /ImageC] >> /Contents " + numConteudo + " 0 R >>\nendobj\n"));

                    offsets.Add(Posicao(ms));
                    EscreverBytes(ms, Encoding.ASCII.GetBytes(numConteudo + " 0 obj\n<< /Length 36 >>\nstream\nq 595.28 0 0 841.89 0 0 cm /Im0 Do Q\nendstream\nendobj\n"));

                    byte[] jpeg;
                    using (var tmp = new MemoryStream())
                    {
                        ImageCodecInfo jpegCodec = null;
                        foreach (ImageCodecInfo c in ImageCodecInfo.GetImageEncoders())
                            if (c.FormatID == ImageFormat.Jpeg.Guid) { jpegCodec = c; break; }
                        var paramsJpeg = new System.Drawing.Imaging.EncoderParameters(1);
                        paramsJpeg.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 92L);
                        if (jpegCodec != null)
                            paginas[i].Save(tmp, jpegCodec, paramsJpeg);
                        else
                            paginas[i].Save(tmp, ImageFormat.Jpeg);
                        jpeg = tmp.ToArray();
                    }

                    offsets.Add(Posicao(ms));
                    EscreverBytes(ms, Encoding.ASCII.GetBytes(numImagem + " 0 obj\n<< /Type /XObject /Subtype /Image /Width " + paginas[i].Width +
                        " /Height " + paginas[i].Height + " /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length " + jpeg.Length + " >>\nstream\n"));
                    ms.Write(jpeg, 0, jpeg.Length);
                    EscreverBytes(ms, Encoding.ASCII.GetBytes("\nendstream\nendobj\n"));
                }

                long xrefOffset = ms.Position;
                var xref = new StringBuilder();
                xref.Append("xref\n0 " + (numObj + 1) + "\n");
                xref.Append("0000000000 65535 f \n");
                for (int i = 0; i < numObj; i++)
                    xref.Append(offsets[i].ToString("0000000000") + " 00000 n \n");
                xref.Append("trailer\n<< /Size " + (numObj + 1) + " /Root 1 0 R >>\nstartxref\n" + xrefOffset + "\n%%EOF");

                EscreverBytes(ms, Encoding.ASCII.GetBytes(xref.ToString()));

                File.WriteAllBytes(caminho, ms.ToArray());
            }
        }

        private static long Posicao(MemoryStream ms)
        {
            return ms.Position;
        }

        private static void EscreverBytes(MemoryStream ms, byte[] bytes)
        {
            ms.Write(bytes, 0, bytes.Length);
        }
    }
}
