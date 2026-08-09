using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Contas a Receber (módulo financeiro).</summary>
    public partial class ContasReceber : BaseForm
    {
        private TextBox txtDescricao;
        private TextBox txtCliente;
        private DateTimePicker dtpVencimento;
        private TextBox txtValor;
        private TextBox txtParcelas;
        private Button btnAdicionar;
        private Button btnReceber;
        private Button btnEditar;
        private Button btnCancelarEdicao;
        private Button btnExcluir;
        private DataGridView grid;
        private ToolStripStatusLabel lblStatus;

        private long? _contaEdicao;
        private ContaFinanceira _baseConta;

        public ContasReceber()
        {
            InitializeComponent();
            Text = "Soen - Contas a Receber";
            ClientSize = new Size(840, 460);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
            txtValor.TextChanged += (s, e) => Validacoes.AplicarMascaraValor(txtValor);
        }

        private void CriarInterface()
        {
            var l1 = new Label { Text = "Descrição:", AutoSize = true, Location = new Point(12, 20) };
            txtDescricao = new TextBox { Location = new Point(90, 17), Size = new Size(300, 20) };

            var l2 = new Label { Text = "Cliente:", AutoSize = true, Location = new Point(400, 20) };
            txtCliente = new TextBox { Location = new Point(460, 17), Size = new Size(220, 20) };

            var l3 = new Label { Text = "Vencimento:", AutoSize = true, Location = new Point(12, 50) };
            dtpVencimento = new DateTimePicker { Location = new Point(90, 47), Size = new Size(110, 20), Format = DateTimePickerFormat.Short };

            var l4 = new Label { Text = "Valor (R$):", AutoSize = true, Location = new Point(230, 50) };
            txtValor = new TextBox { Location = new Point(310, 47), Size = new Size(120, 20) };

            var l5 = new Label { Text = "Parcelas:", AutoSize = true, Location = new Point(460, 50) };
            txtParcelas = new TextBox { Location = new Point(525, 47), Size = new Size(40, 20), Text = "1" };

            btnAdicionar = UIHelpers.CreateButton("Adicionar Conta", new Point(12, 90), new Size(130, 28));
            btnAdicionar.Click += (s, e) => SalvarConta();
            btnReceber = UIHelpers.CreateButton("Marcar Recebido Sel.", new Point(152, 90), new Size(140, 28));
            btnReceber.Click += (s, e) => AlterarStatus("pago");
            btnEditar = UIHelpers.CreateButton("Editar Sel.", new Point(302, 90), new Size(110, 28));
            btnEditar.Click += (s, e) => CarregarParaEdicao();
            btnCancelarEdicao = UIHelpers.CreateButton("Cancelar Edição", new Point(422, 90), new Size(130, 28));
            btnCancelarEdicao.Enabled = false;
            btnCancelarEdicao.Click += (s, e) => CancelarEdicao();
            btnExcluir = UIHelpers.CreateButton("Excluir Sel.", new Point(562, 90), new Size(110, 28));
            btnExcluir.Click += (s, e) => Excluir();

            grid = new DataGridView
            {
                Location = new Point(12, 132),
                Size = new Size(812, 304),
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            grid.Columns.Add("Id", "Nº");
            grid.Columns.Add("Descricao", "Descrição");
            grid.Columns.Add("Cliente", "Cliente");
            grid.Columns.Add("Vencimento", "Vencimento");
            grid.Columns.Add("Valor", "Valor");
            grid.Columns.Add("Pagamento", "Pagamento");
            grid.Columns.Add("Status", "Status");

            // usa StatusStrip padrão da BaseForm
            lblStatus = BaseStatusLabel;

            Controls.AddRange(new Control[] { l1, txtDescricao, l2, txtCliente, l3, dtpVencimento, l4, txtValor,
                l5, txtParcelas, btnAdicionar, btnReceber, btnEditar, btnCancelarEdicao, btnExcluir, grid });
        }

        private void Carregar()
        {
            grid.Rows.Clear();
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            List<ContaFinanceira> lista = FinanceiroDAO.Listar("receber");
            foreach (var c in lista)
                grid.Rows.Add(c.Id, c.Descricao, c.Fornecedor, c.Vencimento, c.Valor.ToString("N2", cult),
                    c.Status == "pago" ? c.DataPagamento : "", c.Status);
            lblStatus.Text = lista.Count + " conta(s) a receber - Em aberto: R$ " +
                FinanceiroDAO.TotalAberto("receber").ToString("N2", cult);
        }

        private void SalvarConta()
        {
            if (_contaEdicao.HasValue) { SalvarEdicao(); return; }

            double valor;
            double.TryParse(txtValor.Text, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out valor);
            if (valor <= 0)
            {
                MessageBox.Show("Informe um valor válido.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int parcelas;
            if (!int.TryParse(txtParcelas.Text, out parcelas) || parcelas < 1)
            {
                MessageBox.Show("Informe a quantidade de parcelas (mínimo 1).", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DateTime venc = dtpVencimento.Value.Date;
            if (parcelas == 1)
            {
                SalvarParcela(1, parcelas, venc, valor);
            }
            else
            {
                double porParcela = Math.Round(valor / parcelas, 2);
                double totalParcelas = porParcela * parcelas;
                double ajuste = Math.Round(valor - totalParcelas, 2);
                for (int i = 0; i < parcelas; i++)
                {
                    double v = (i == parcelas - 1) ? porParcela + ajuste : porParcela;
                    SalvarParcela(i + 1, parcelas, venc.AddMonths(i), v);
                }
            }

            Carregar();
            LimparCampos(false);
        }

        private void SalvarParcela(int numero, int total, DateTime venc, double valor)
        {
            var c = new ContaFinanceira
            {
                Tipo = "receber",
                Descricao = txtDescricao.Text.Trim() + (total > 1 ? " (parc. " + numero + "/" + total + ")" : ""),
                Fornecedor = txtCliente.Text.Trim(),
                Vencimento = venc.ToString("yyyy-MM-dd"),
                Valor = valor
            };
            FinanceiroDAO.Salvar(c);
        }

        private void CarregarParaEdicao()
        {
            var id = ContaSelecionada();
            if (!id.HasValue) return;
            List<ContaFinanceira> lista = FinanceiroDAO.Listar("receber");
            foreach (var c in lista)
            {
                if (c.Id != id.Value) continue;
                _contaEdicao = c.Id;
                _baseConta = c;
                txtDescricao.Text = c.Descricao;
                txtCliente.Text = c.Fornecedor;
                DateTime d;
                if (DateTime.TryParse(c.Vencimento, out d))
                    dtpVencimento.Value = d;
                txtValor.Text = c.Valor.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"));
                txtParcelas.Enabled = false;
                btnAdicionar.Text = "Salvar Alterações";
                btnCancelarEdicao.Enabled = true;
                break;
            }
        }

        private void CancelarEdicao()
        {
            LimparCampos(true);
        }

        private void LimparCampos(bool cancelarEdicao)
        {
            txtDescricao.Clear();
            txtCliente.Clear();
            txtValor.Clear();
            if (cancelarEdicao)
            {
                _contaEdicao = null;
                _baseConta = null;
                txtParcelas.Enabled = true;
                txtParcelas.Text = "1";
                btnAdicionar.Text = "Adicionar Conta";
                btnCancelarEdicao.Enabled = false;
            }
        }

        private void SalvarEdicao()
        {
            double valor;
            if (!double.TryParse(txtValor.Text, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out valor) || valor <= 0)
            {
                MessageBox.Show("Informe um valor válido.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var c = new ContaFinanceira
            {
                Id = _contaEdicao.Value,
                Tipo = "receber",
                Descricao = txtDescricao.Text.Trim(),
                Fornecedor = txtCliente.Text.Trim(),
                Vencimento = dtpVencimento.Value.ToString("yyyy-MM-dd"),
                Valor = valor,
                Status = _baseConta?.Status ?? "em_aberto",
                DataPagamento = _baseConta?.DataPagamento
            };
            FinanceiroDAO.Salvar(c);
            LimparCampos(true);
            Carregar();
        }

        private void AlterarStatus(string status)
        {
            var id = ContaSelecionada();
            if (!id.HasValue) return;
            List<ContaFinanceira> lista = FinanceiroDAO.Listar("receber");
            foreach (var c in lista)
            {
                if (c.Id == id.Value)
                {
                    c.Status = status;
                    if (status == "pago" && string.IsNullOrWhiteSpace(c.DataPagamento))
                        c.DataPagamento = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    FinanceiroDAO.Salvar(c);
                    break;
                }
            }
            Carregar();
        }

        private long? ContaSelecionada()
        {
            if (grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione uma conta na lista.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }
            return Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
        }

        private void Excluir()
        {
            var id = ContaSelecionada();
            if (!id.HasValue) return;
            if (MessageBox.Show("Excluir esta conta?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                FinanceiroDAO.Excluir(id.Value);
                Carregar();
            }
        }
    }
}