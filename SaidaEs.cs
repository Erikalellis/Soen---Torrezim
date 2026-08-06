using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>
    /// Saída de Estoque: escolhe um produto, informa a quantidade e registra
    /// a saída, diminuindo da quantidade disponível (valida saldo suficiente).
    /// </summary>
    public partial class SaidaEs : Form
    {
        private ComboBox cmbProduto;
        private TextBox txtQuantidade;
        private TextBox txtDocumento;
        private Button btnRegistrar;
        private DataGridView grid;
        private Label lblQtdAtual;

        public SaidaEs()
        {
            InitializeComponent();
            Text = "Soen - Saída de Estoque";
            ClientSize = new Size(760, 470);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;

            CriarInterface();
            CarregarProdutos();
            CarregarMovimentacoes();
        }

        private void CriarInterface()
        {
            var lProd = new Label { Text = "Produto:", AutoSize = true, Location = new Point(12, 20) };
            cmbProduto = new ComboBox { Location = new Point(70, 16), Size = new Size(420, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProduto.SelectedIndexChanged += (s, e) => AtualizarQtdAtual();

            var lQtd = new Label { Text = "Quantidade:", AutoSize = true, Location = new Point(510, 20) };
            txtQuantidade = new TextBox { Location = new Point(595, 16), Size = new Size(70, 20), Text = "1" };

            btnRegistrar = new Button { Text = "Registrar Saída", Location = new Point(680, 13), Size = new Size(120, 26), BackColor = SystemColors.AppWorkspace };
            btnRegistrar.Click += (s, e) => Registrar();

            var lDoc = new Label { Text = "Documento (opcional):", AutoSize = true, Location = new Point(12, 52) };
            txtDocumento = new TextBox { Location = new Point(70, 60), Size = new Size(420, 20) };

            lblQtdAtual = new Label { AutoSize = true, Location = new Point(510, 63), ForeColor = Color.DarkRed };

            grid = new DataGridView
            {
                Location = new Point(12, 92),
                Size = new Size(736, 340),
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            grid.Columns.Add("Data", "Data");
            grid.Columns.Add("Produto", "Produto");
            grid.Columns.Add("Qtd", "Quantidade");
            grid.Columns.Add("Doc", "Documento");
            grid.Columns["Produto"].FillWeight = 3f;

            Controls.AddRange(new Control[] { lProd, cmbProduto, lQtd, txtQuantidade, btnRegistrar,
                lDoc, txtDocumento, lblQtdAtual, grid });
        }

        private void CarregarProdutos()
        {
            cmbProduto.Items.Clear();
            cmbProduto.SelectedIndex = -1;
            foreach (Produto p in ProdutoDAO.Listar(""))
            {
                cmbProduto.Items.Add(new ComboProduto { Id = p.Id, Nome = p.Nome, Qtd = p.QtdAtual });
            }
            if (cmbProduto.Items.Count > 0) cmbProduto.SelectedIndex = 0;
        }

        private void AtualizarQtdAtual()
        {
            var p = cmbProduto.SelectedItem as ComboProduto;
            lblQtdAtual.Text = p == null ? "" : ("Estoque atual: " + p.Qtd.ToString("0.##", CultureInfo.GetCultureInfo("pt-BR")));
        }

        private void Registrar()
        {
            var p = cmbProduto.SelectedItem as ComboProduto;
            if (p == null) { MessageBox.Show("Selecione um produto.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            double qtd;
            if (!double.TryParse(txtQuantidade.Text, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out qtd) || qtd <= 0)
            {
                MessageBox.Show("Informe uma quantidade válida.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (qtd > p.Qtd)
            {
                MessageBox.Show("Estoque insuficiente. Disponível: " + p.Qtd.ToString("0.##", CultureInfo.GetCultureInfo("pt-BR")),
                    "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ProdutoDAO.Movimentar(p.Id, "saida", qtd, txtDocumento.Text.Trim());
                CarregarProdutos();
                CarregarMovimentacoes();
                txtQuantidade.Text = "1";
                txtDocumento.Clear();
                MessageBox.Show("Saída registrada!", "SOEN", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao registrar: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CarregarMovimentacoes()
        {
            grid.Rows.Clear();
            using (var conn = Database.AbrirConexao())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT m.data, p.nome, m.quantidade, m.documento
FROM movimentacao_estoque m JOIN produtos p ON p.id=m.produto_id
WHERE m.tipo='saida' ORDER BY m.id DESC";
                using (var r = cmd.ExecuteReader())
                {
                    var cult = CultureInfo.GetCultureInfo("pt-BR");
                    while (r.Read())
                    {
                        string doc = r.IsDBNull(r.GetOrdinal("documento")) ? "" : r.GetString(r.GetOrdinal("documento"));
                        grid.Rows.Add(r["data"] != System.DBNull.Value ? r["data"].ToString() : "",
                            r["nome"] != null ? r["nome"].ToString() : "",
                            Convert.ToDouble(r["quantidade"]).ToString("0.##", cult), doc);
                    }
                }
            }
        }
    }
}