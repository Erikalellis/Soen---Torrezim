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
    /// Inventário de peças e materiais: cadastro de produtos, ajuste de estoque
    /// e listagem com quantidade disponível.
    /// </summary>
    public partial class InventarioPM : BaseForm
    {
        private TextBox txtBusca;
        private Button btnBuscar;
        private Button btnTodos;
        private DataGridView grid;
        private TextBox txtCodigo;
        private TextBox txtNome;
        private TextBox txtCategoria;
        private TextBox txtUnidade;
        private TextBox txtCusto;
        private TextBox txtPreco;
        private Button btnSalvar;
        private Button btnNovo;
        private Button btnExcluir;
        private ToolStripStatusLabel lblStatus;

        private long? _produtoEdicao;
        private bool _alertaMostrado;

        public InventarioPM()
        {
            InitializeComponent();
            Text = "Soen - Inventário de Peças e Materiais";
            ClientSize = new Size(860, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;

            CriarInterface();
            Carregar("");
        }

        private void CriarInterface()
        {
            // Busca
            txtBusca = new TextBox { Location = new Point(12, 12), Size = new Size(250, 22) };
            txtBusca.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Carregar(txtBusca.Text.Trim()); };
            btnBuscar = Botao("Buscar", 270, 8, (s, e) => Carregar(txtBusca.Text.Trim()));
            btnTodos = Botao("Todos", 368, 8, (s, e) => { txtBusca.Clear(); Carregar(""); });

            // Formulário do produto
            int y = 40;
            var l1 = new Label { Text = "Código:", AutoSize = true, Location = new Point(12, y + 3) };
            txtCodigo = new TextBox { Location = new Point(70, y), Size = new Size(120, 20) };

            var l2 = new Label { Text = "Nome:", AutoSize = true, Location = new Point(210, y + 3) };
            txtNome = new TextBox { Location = new Point(270, y), Size = new Size(340, 20) };
            y += 30;

            var l3 = new Label { Text = "Categoria:", AutoSize = true, Location = new Point(12, y + 3) };
            txtCategoria = new TextBox { Location = new Point(90, y), Size = new Size(180, 20) };

            var l4 = new Label { Text = "Unidade:", AutoSize = true, Location = new Point(310, y + 3) };
            txtUnidade = new TextBox { Location = new Point(370, y), Size = new Size(70, 20), Text = "un" };

            var l5 = new Label { Text = "Custo R$:", AutoSize = true, Location = new Point(480, y + 3) };
            txtCusto = new TextBox { Location = new Point(545, y), Size = new Size(80, 20) };

            var l6 = new Label { Text = "Preço R$:", AutoSize = true, Location = new Point(650, y + 3) };
            txtPreco = new TextBox { Location = new Point(720, y), Size = new Size(90, 20) };
            y += 32;

            btnSalvar = Botao("Salvar Produto", 12, y, (s, e) => SalvarProduto());
            btnNovo = Botao("Limpar", 120, y, (s, e) => LimparFormularioProduto());
            btnExcluir = Botao("Excluir Selecionado", 210, y, (s, e) => ExcluirProduto());
            y += 34;

            grid = new DataGridView
            {
                Location = new Point(12, y),
                Size = new Size(900, 300),
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            grid.Columns.Add("Id", "Código");
            grid.Columns.Add("Nome", "Nome");
            grid.Columns.Add("Categoria", "Categoria");
            grid.Columns.Add("Qtd", "Qtd");
            grid.Columns.Add("Unidade", "Unid.");
            grid.Columns.Add("Custo", "Custo R$");
            grid.Columns.Add("Preco", "Preço R$");
            grid.Columns["Nome"].FillWeight = 3.5f;
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) CarregarParaEdicao(); };

            // usa StatusStrip padrão da BaseForm
            lblStatus = BaseStatusLabel;

            Controls.AddRange(new Control[] { txtBusca, btnBuscar, btnTodos, l1, txtCodigo, l2, txtNome,
                l3, txtCategoria, l4, txtUnidade, l5, txtCusto, l6, txtPreco,
                btnSalvar, btnNovo, btnExcluir, grid });
        }

        private Button Botao(string texto, int x, int y, EventHandler clique = null)
        {
            var b = new Button { Text = texto, Location = new Point(x, y), Size = new Size(100, 26), BackColor = SystemColors.AppWorkspace };
            if (clique != null) b.Click += clique;
            return b;
        }

        private void Carregar(string filtro)
        {
            grid.Rows.Clear();
            List<Produto> lista = ProdutoDAO.Listar(filtro);
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            int baixo = 0;
            foreach (var p in lista)
            {
                int idx = grid.Rows.Add(p.Id, p.Nome, p.Categoria, p.QtdAtual.ToString("0.##", cult), p.Unidade,
                    p.Custo.ToString("N2", cult), p.Preco.ToString("N2", cult));
                if (p.QtdAtual <= 5)
                {
                    grid.Rows[idx].DefaultCellStyle.BackColor = Color.LightSalmon;
                    grid.Rows[idx].DefaultCellStyle.SelectionBackColor = Color.IndianRed;
                    baixo++;
                }
            }
            lblStatus.Text = lista.Count + " produto(s) no estoque." + (baixo > 0 ? " • ALERTA: " + baixo + " com estoque baixo (≤5)." : "");

            if (baixo > 0 && !_alertaMostrado)
            {
                _alertaMostrado = true;
                MessageBox.Show("Atenção: " + baixo + " produto(s) com estoque baixo (qtd ≤ 5).\nReponha o estoque para evitar falta de peças.",
                    "Estoque baixo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SalvarProduto()
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show("Informe o nome do produto.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                var p = new Produto
                {
                    Id = _produtoEdicao ?? 0,
                    Codigo = txtCodigo.Text.Trim(),
                    Nome = txtNome.Text.Trim(),
                    Categoria = txtCategoria.Text.Trim(),
                    Unidade = string.IsNullOrWhiteSpace(txtUnidade.Text) ? "un" : txtUnidade.Text.Trim(),
                    Custo = Parse(txtCusto.Text),
                    Preco = Parse(txtPreco.Text)
                };
                ProdutoDAO.Salvar(p);
                LimparFormularioProduto();
                Carregar(txtBusca.Text.Trim());
                MessageBox.Show("Produto salvo!", "SOEN", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExcluirProduto()
        {
            if (grid.SelectedRows.Count == 0) return;
            var id = Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
            if (MessageBox.Show("Excluir este produto (e seu histórico de movimentações)?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ProdutoDAO.Excluir(id);
                LimparFormularioProduto();
                Carregar(txtBusca.Text.Trim());
            }
        }

        private void CarregarParaEdicao()
        {
            if (grid.SelectedRows.Count == 0) return;
            var id = Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
            Produto p = ProdutoDAO.BuscarPorId(id);
            if (p == null) return;
            _produtoEdicao = p.Id;
            txtCodigo.Text = p.Codigo;
            txtNome.Text = p.Nome;
            txtCategoria.Text = p.Categoria;
            txtUnidade.Text = p.Unidade;
            txtCusto.Text = p.Custo.ToString(CultureInfo.GetCultureInfo("pt-BR"));
            txtPreco.Text = p.Preco.ToString(CultureInfo.GetCultureInfo("pt-BR"));
        }

        private void LimparFormularioProduto()
        {
            _produtoEdicao = null;
            txtCodigo.Clear(); txtNome.Clear(); txtCategoria.Clear(); txtUnidade.Text = "un";
            txtCusto.Clear(); txtPreco.Clear();
            txtNome.Focus();
        }

        private static double Parse(string s)
        {
            double v;
            double.TryParse(s, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out v);
            return v;
        }
    }
}