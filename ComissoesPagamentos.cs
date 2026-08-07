using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Soen___Torrezim.Data;

namespace Soen___Torrezim
{
    /// <summary>
    /// Controle de comissões por técnico com registro de pagamentos: mostra o
    /// saldo (comissões geradas pelas OS convertidas menos os valores pagos) e o
    /// histórico de pagamentos do técnico selecionado.
    /// </summary>
    public partial class ComissoesPagamentos : Form
    {
        private DataGridView gridResumo;
        private DataGridView gridHistorico;
        private Label lblSaldoTotal;
        private Label lblHistorico;
        private Button btnRegistrar;
        private Button btnExcluir;
        private Button btnAtualizar;

        public ComissoesPagamentos()
        {
            InitializeComponent();
            Text = "Soen - Pagamentos de Comissões por Técnico";
            ClientSize = new Size(820, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            Carregar();
        }

        private void CriarInterface()
        {
            lblSaldoTotal = new Label { Text = "", AutoSize = true, Location = new Point(12, 12), Font = new Font("Segoe UI", 9, FontStyle.Bold) };

            gridResumo = CriarGrid(new Point(12, 40), new Size(796, 170));
            gridResumo.Columns.Add("TecnicoId", "ID");
            gridResumo.Columns.Add("Tecnico", "Técnico");
            gridResumo.Columns.Add("Gerado", "Comissões geradas");
            gridResumo.Columns.Add("Pago", "Total pago");
            gridResumo.Columns.Add("Saldo", "Saldo");
            gridResumo.Columns["TecnicoId"].Visible = false;
            gridResumo.SelectionChanged += (s, e) => CarregarHistorico();

            var lFiltro = new Label { Text = "Selecione um técnico para ver os pagamentos:", AutoSize = true, Location = new Point(12, 216) };

            btnRegistrar = UIHelpers.CreateButton("Registrar Pagamento", new Point(12, 236), new Size(150, 28));
            btnRegistrar.Click += (s, e) => RegistrarPagamento();
            btnExcluir = UIHelpers.CreateButton("Excluir Pagamento", new Point(664, 236), new Size(144, 28));
            btnExcluir.Click += (s, e) => ExcluirPagamento();
            btnAtualizar = UIHelpers.CreateButton("Atualizar", new Point(170, 236), new Size(110, 28));
            btnAtualizar.Click += (s, e) => Carregar();

            lblHistorico = new Label { Text = "Histórico de pagamentos:", AutoSize = true, Location = new Point(12, 270) };

            gridHistorico = CriarGrid(new Point(12, 294), new Size(796, 190));
            gridHistorico.Columns.Add("Id", "ID");
            gridHistorico.Columns.Add("Data", "Data");
            gridHistorico.Columns.Add("Tecnico", "Técnico");
            gridHistorico.Columns.Add("Valor", "Valor");
            gridHistorico.Columns.Add("Obs", "Observação");
            gridHistorico.Columns["Id"].Visible = false;

            Controls.AddRange(new Control[] { lblSaldoTotal, gridResumo, lFiltro, btnRegistrar, btnExcluir, btnAtualizar, lblHistorico, gridHistorico });
        }

        private static DataGridView CriarGrid(Point location, Size size)
        {
            return new DataGridView
            {
                Location = location,
                Size = size,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
        }

        private void Carregar()
        {
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            gridResumo.Rows.Clear();
            double saldoTotal = 0;
            foreach (var r in TecnicoDAO.ListarResumoComissoes())
            {
                gridResumo.Rows.Add(r.TecnicoId, r.NomeTecnico,
                    r.Gerado.ToString("N2", cult), r.Pago.ToString("N2", cult),
                    r.Saldo.ToString("N2", cult));
                saldoTotal += r.Saldo;
            }
            lblSaldoTotal.Text = "Saldo total a pagar em comissões: R$ " + saldoTotal.ToString("N2", cult);
            CarregarHistorico();
        }

        private void CarregarHistorico()
        {
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            gridHistorico.Rows.Clear();
            long? tecnicoId = null;
            if (gridResumo.SelectedRows.Count > 0)
                tecnicoId = Convert.ToInt64(gridResumo.SelectedRows[0].Cells["TecnicoId"].Value);

            foreach (var p in TecnicoDAO.ListarPagamentos(tecnicoId))
                gridHistorico.Rows.Add(p.Id, p.Data, p.NomeTecnico, p.Valor.ToString("N2", cult), p.Observacao);
        }

        private void RegistrarPagamento()
        {
            if (gridResumo.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um técnico na lista acima.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            long tecnicoId = Convert.ToInt64(gridResumo.SelectedRows[0].Cells["TecnicoId"].Value);
            string nome = gridResumo.SelectedRows[0].Cells["Tecnico"].Value.ToString();
            double saldo;
            if (!double.TryParse(gridResumo.SelectedRows[0].Cells["Saldo"].Value?.ToString(),
                NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out saldo))
            {
                MessageBox.Show("Não foi possível ler o saldo do técnico.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double valor; string obs;
            if (!PedirValor(nome, saldo, out valor, out obs)) return;
            if (valor <= 0)
            {
                MessageBox.Show("Informe um valor maior que zero.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (valor > saldo + 0.001)
            {
                MessageBox.Show("O valor não pode ultrapassar o saldo (R$ " + saldo.ToString("N2") + ").",
                    "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            TecnicoDAO.RegistrarPagamento(tecnicoId, valor, obs);
            Carregar();
            MessageBox.Show("Pagamento registrado com sucesso.", "SOEN", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExcluirPagamento()
        {
            if (gridHistorico.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um pagamento no histórico.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            long id = Convert.ToInt64(gridHistorico.SelectedRows[0].Cells["Id"].Value);
            if (MessageBox.Show("Excluir este pagamento?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            TecnicoDAO.ExcluirPagamento(id);
            Carregar();
        }

        private static bool PedirValor(string nomeTecnico, double saldo, out double valor, out string observacao)
        {
            valor = 0; observacao = "";
            var dlg = new Form
            {
                Text = "Registrar Pagamento de Comissão",
                ClientSize = new Size(360, 180),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            var lNome = new Label { Text = "Técnico: " + nomeTecnico, Location = new Point(12, 12), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            var lValor = new Label { Text = "Valor (R$):", Location = new Point(12, 40), AutoSize = true };
            var txtValor = new TextBox { Location = new Point(120, 36), Width = 120 };
            var lObs = new Label { Text = "Observação:", Location = new Point(12, 68), AutoSize = true };
            var txtObs = new TextBox { Location = new Point(120, 64), Width = 220 };
            var bOk = UIHelpers.CreateButton("OK", new Point(150, 140), new Size(90, 28));
            var bCanc = UIHelpers.CreateButton("Cancelar", new Point(248, 140), new Size(90, 28));
            bOk.Click += (s, e) => { valor = _ParseValor(txtValor.Text); observacao = txtObs.Text.Trim(); dlg.DialogResult = DialogResult.OK; };
            bCanc.Click += (s, e) => dlg.DialogResult = DialogResult.Cancel;
            dlg.Controls.AddRange(new Control[] { lNome, lValor, txtValor, lObs, txtObs, bOk, bCanc });
            return dlg.ShowDialog() == DialogResult.OK;
        }

        private static double _ParseValor(string texto)
        {
            double v;
            double.TryParse(texto, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out v);
            return v;
        }
    }
}