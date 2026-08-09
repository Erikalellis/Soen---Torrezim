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
    /// Controle de Caixa: mostra todos os lançamentos (entradas e saídas),
    /// o saldo atual e permite lançar entradas/saídas manuais.
    /// </summary>
    public partial class ControleCaixa : BaseForm
    {
        private DataGridView grid;
        private ComboBox cmbTipo;
        private TextBox txtDescricao;
        private TextBox txtValor;
        private Button btnLancar;
        private Button btnExcluir;
        private Button btnRecibo;
        private Button btnFechamento;
        private Button btnReabrir;
        private Label lblSaldo;
        private ToolStripStatusLabel lblStatus;

        public ControleCaixa()
        {
            InitializeComponent();
            Text = "Soen - Controle de Caixa";
            ClientSize = new Size(760, 460);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;

            CriarInterface();
            CarregarLancamentos();
        }

        private void CriarInterface()
        {
            var lblTipo = new Label { Text = "Tipo:", AutoSize = true, Location = new Point(12, 20) };
            cmbTipo = new ComboBox { Location = new Point(50, 17), Size = new Size(100, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTipo.Items.AddRange(new object[] { "entrada", "saida" });
            cmbTipo.SelectedIndex = 0;

            var lblDesc = new Label { Text = "Descrição:", AutoSize = true, Location = new Point(160, 20) };
            txtDescricao = new TextBox { Location = new Point(230, 17), Size = new Size(220, 20) };

            var lblValor = new Label { Text = "Valor (R$):", AutoSize = true, Location = new Point(460, 20) };
            txtValor = new TextBox { Location = new Point(530, 17), Size = new Size(90, 20) };

            btnLancar = UIHelpers.CreateButton("Lançar", new Point(630, 13), new Size(100, 26));
            btnLancar.Click += (s, e) => Lancar();

            grid = new DataGridView
            {
                Location = new Point(12, 52),
                Size = new Size(736, 340),
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            grid.Columns.Add("Id", "Código");
            grid.Columns.Add("Data", "Data");
            grid.Columns.Add("Tipo", "Tipo");
            grid.Columns.Add("Descricao", "Descrição");
            grid.Columns.Add("Valor", "Valor (R$)");
            grid.Columns["Descricao"].FillWeight = 4f;

            btnExcluir = UIHelpers.CreateButton("Excluir Selecionado", new Point(612, 400), new Size(136, 26));
            btnExcluir.Click += (s, e) => Excluir();

            btnRecibo = UIHelpers.CreateButton("Recibo do Lançamento", new Point(468, 400), new Size(136, 26));
            btnRecibo.Click += (s, e) => ImprimirReciboCaixa();

            btnReabrir = UIHelpers.CreateButton("Reabrir Dia", new Point(180, 400), new Size(136, 26));
            btnReabrir.Click += (s, e) => ReabrirDia();

            btnFechamento = UIHelpers.CreateButton("Fechamento do Dia", new Point(324, 400), new Size(136, 26));
            btnFechamento.Click += (s, e) => ImprimirFechamentoDia();

            lblSaldo = new Label
            {
                AutoSize = true,
                Location = new Point(12, 404),
                Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold)
            };

            // usa StatusStrip padrão da BaseForm
            lblStatus = BaseStatusLabel;

            Controls.AddRange(new Control[] { lblTipo, cmbTipo, lblDesc, txtDescricao, lblValor, txtValor, btnLancar, grid, btnExcluir, btnRecibo, btnReabrir, btnFechamento, lblSaldo });
        }

        private void CarregarLancamentos()
        {
            grid.Rows.Clear();
            List<LancamentoCaixa> lista = CaixaDAO.Listar();
            var cult = CultureInfo.GetCultureInfo("pt-BR");

            double entradas = 0, saidas = 0;
            foreach (var l in lista)
            {
                grid.Rows.Add(l.Id, l.Data, l.Tipo, l.Descricao, l.Valor.ToString("N2", cult));
                if (l.Tipo == "entrada") entradas += l.Valor; else saidas += l.Valor;
            }

            double saldo = CaixaDAO.Saldo();
            lblSaldo.Text = "SALDO ATUAL: R$ " + saldo.ToString("N2", cult);
            lblSaldo.ForeColor = saldo >= 0 ? Color.DarkGreen : Color.Firebrick;
            lblStatus.Text = lista.Count + " lançamento(s) • Entradas R$ " + entradas.ToString("N2", cult) + " • Saídas R$ " + saidas.ToString("N2", cult);
        }

        /// <summary>Chave "caixa_fechado" na tabela config guarda o último dia fechado (yyyy-MM-dd).</summary>
        private static string DiaFechado
        {
            get { return ConfigDAO.Obter("caixa_fechado", ""); }
            set { ConfigDAO.Salvar("caixa_fechado", value); }
        }

        private void Lancar()
        {
            string hoje = DateTime.Now.ToString("yyyy-MM-dd");
            if (DiaFechado == hoje)
            {
                MessageBox.Show("O caixa de hoje já foi fechado. Para lançar neste dia, use \"Reabrir Dia\".",
                    "Dia fechado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtDescricao.Text))
            {
                MessageBox.Show("Informe a descrição do lançamento.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double valor;
            if (!double.TryParse(txtValor.Text, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out valor) || valor <= 0)
            {
                MessageBox.Show("Informe um valor válido.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                CaixaDAO.Salvar(new LancamentoCaixa
                {
                    Data = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    Tipo = cmbTipo.SelectedItem?.ToString() ?? "entrada",
                    Descricao = txtDescricao.Text.Trim(),
                    Valor = valor
                });

                txtDescricao.Clear();
                txtValor.Clear();
                CarregarLancamentos();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                MessageBox.Show("Erro ao lançar. Veja o log para detalhes.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Excluir()
        {
            if (grid.SelectedRows.Count == 0) return;
            var id = Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
            if (MessageBox.Show("Excluir este lançamento?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                CaixaDAO.Excluir(id);
                CarregarLancamentos();
            }
        }

        private void ImprimirReciboCaixa()
        {
            if (grid.SelectedRows.Count == 0) return;
            var id = Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
            LancamentoCaixa l = null;
            foreach (var x in CaixaDAO.Listar())
                if (x.Id == id) { l = x; break; }
            if (l == null) return;
            RelatorioHelper.VisualizarReciboCaixa(l);
        }

        private void ImprimirFechamentoDia()
        {
            var hoje = DateTime.Now;
            string dia = hoje.ToString("yyyy-MM-dd");

            if (DiaFechado == dia)
            {
                MessageBox.Show("Este dia já foi fechado. O resumo é exibido apenas para consulta.",
                    "Dia já fechado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (MessageBox.Show("Fechar o caixa de hoje (" + hoje.ToShortDateString() +
                ")? Após fechar, novos lançamentos deste dia serão bloqueados.",
                "Fechar o dia", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                DiaFechado = dia;
                CarregarLancamentos();
                MessageBox.Show("Dia fechado com sucesso.", "Fechamento do caixa",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            var movs = CaixaDAO.ListarPorDia(dia);
            RelatorioHelper.VisualizarResumoCaixa(hoje, movs);
        }

        private void ReabrirDia()
        {
            string dia = DateTime.Now.ToString("yyyy-MM-dd");
            if (DiaFechado == dia &&
                MessageBox.Show("Reabrir o caixa de hoje para novos lançamentos?",
                    "Reabrir dia", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                DiaFechado = "";
                CarregarLancamentos();
                MessageBox.Show("Dia reaberto.", "Controle de Caixa",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}