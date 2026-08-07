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
    /// Registro de Vendas: escolhe cliente (e veículo), adiciona itens
    /// (descrição + quantidade + valor unitário), calcula o total e salva.
    /// Ao salvar, a venda entra automaticamente no caixa como entrada.
    /// </summary>
    public partial class RegistroVendas : BaseForm
    {
        private ComboBox cmbCliente;
        private ComboBox cmbVeiculo;
        private TextBox txtItem;
        private TextBox txtQtd;
        private TextBox txtValorUnit;
        private Button btnAdicionar;
        private DataGridView gridItens;
        private Button btnRemoverItem;
        private TextBox txtTotal;
        private ComboBox cmbForma;
        private Button btnSalvar;
        private Button btnNovo;
        private Button btnExcluir;
        private Button btnRecibo;
        private DataGridView gridVendas;
        private ToolStripStatusLabel lblStatus;

        private readonly List<VendaItem> itensAtuais = new List<VendaItem>();
        private long? _vendaId; // não-nulo = editando uma venda

        public RegistroVendas()
        {
            InitializeComponent();
            Text = "Soen - Registro de Vendas";
            ClientSize = new Size(900, 620);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;

            CriarInterface();
            CarregarClientes();
            CarregarVendas();
            txtValorUnit.TextChanged += (s, e) => Validacoes.AplicarMascaraValor(txtValorUnit);
        }

        private void CriarInterface()
        {
            int y = 14, lx = 12, fx = 130;

            var lblCli = new Label { Text = "Cliente:", AutoSize = true, Location = new Point(lx, y + 3) };
            cmbCliente = new ComboBox { Location = new Point(fx, y), Size = new Size(320, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCliente.SelectedIndexChanged += (s, e) => CarregarVeiculos();

            var lblVeic = new Label { Text = "Veículo:", AutoSize = true, Location = new Point(480, y + 3) };
            cmbVeiculo = new ComboBox { Location = new Point(560, y), Size = new Size(320, 21), DropDownStyle = ComboBoxStyle.DropDownList };

            y += 38;
            var lblItem = new Label { Text = "Item / Serviço:", AutoSize = true, Location = new Point(lx, y + 3) };
            txtItem = new TextBox { Location = new Point(fx, y), Size = new Size(360, 20) };

            var lblQtd = new Label { Text = "Qtd:", AutoSize = true, Location = new Point(510, y + 3) };
            txtQtd = new TextBox { Location = new Point(545, y), Size = new Size(50, 20), Text = "1" };

            var lblVu = new Label { Text = "R$ unit:", AutoSize = true, Location = new Point(620, y + 3) };
            txtValorUnit = new TextBox { Location = new Point(685, y), Size = new Size(90, 20) };

            btnAdicionar = UIHelpers.CreateButton("Adicionar", new Point(790, y - 3), new Size(90, 26));
            btnAdicionar.Click += (s, e) => AdicionarItem();

            y += 34;
            gridItens = NovoGrid();
            gridItens.Location = new Point(lx, y);
            gridItens.Size = new Size(870, 150);
            gridItens.Columns.Add("Desc", "Item / Serviço");
            gridItens.Columns.Add("Qtd", "Qtd");
            gridItens.Columns.Add("Unit", "Valor Unit");
            gridItens.Columns.Add("Sub", "Subtotal");
            gridItens.Columns["Desc"].FillWeight = 5f;
            gridItens.Columns["Qtd"].FillWeight = 1f;
            gridItens.Columns["Unit"].FillWeight = 1.5f;
            gridItens.Columns["Sub"].FillWeight = 1.5f;
            gridItens.Columns[0].ReadOnly = false;

            y += 160;
            btnRemoverItem = UIHelpers.CreateButton("Remover Item Selecionado", new Point(lx, y), new Size(180, 26));
            btnRemoverItem.Click += (s, e) => RemoverItem();

            var lblTotal = new Label { Text = "TOTAL: R$", AutoSize = true, Location = new Point(660, y + 3) };
            txtTotal = new TextBox { Location = new Point(730, y), Size = new Size(150, 22), ReadOnly = true, Font = new Font("Microsoft Sans Serif", 9.5F, FontStyle.Bold), Text = "0,00" };

            y += 36;
            var lblForma = new Label { Text = "Forma de pagamento:", AutoSize = true, Location = new Point(lx, y + 3) };
            cmbForma = new ComboBox { Location = new Point(fx + 120, y), Size = new Size(200, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbForma.Items.AddRange(new object[] { "Dinheiro", "Cartão", "Pix", "Boleto", "Fiado" });
            cmbForma.SelectedIndex = 0;

            btnSalvar = UIHelpers.CreateButton("Salvar Venda", new Point(560, y - 3), new Size(110, 26));
            btnSalvar.Click += (s, e) => SalvarVenda();

            btnNovo = UIHelpers.CreateButton("Nova Venda", new Point(680, y - 3), new Size(110, 26));
            btnNovo.Click += (s, e) => LimparFormulario();

            y += 40;
            var lblHist = new Label { Text = "Vendas registradas:", AutoSize = true, Location = new Point(lx, y) };
            y += 22;
            gridVendas = NovoGrid();
            gridVendas.Location = new Point(lx, y);
            gridVendas.Size = new Size(870, 180);
            gridVendas.Columns.Add("Id", "Nº");
            gridVendas.Columns.Add("Data", "Data");
            gridVendas.Columns.Add("Cliente", "Cliente");
            gridVendas.Columns.Add("Veiculo", "Veículo");
            gridVendas.Columns.Add("Total", "Total");
            gridVendas.Columns.Add("Forma", "Forma");
            gridVendas.Columns["Cliente"].FillWeight = 2.5f;
            gridVendas.Columns["Total"].FillWeight = 1.2f;
            gridVendas.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) CarregarVendaParaEdicao(); };

            btnExcluir = UIHelpers.CreateButton("Excluir Selecionada", new Point(760, y + 185), new Size(120, 26));
            btnExcluir.Click += (s, e) => ExcluirVenda();

            btnRecibo = UIHelpers.CreateButton("Recibo / Nota", new Point(622, y + 185), new Size(120, 26));
            btnRecibo.Click += (s, e) => ImprimirReciboVenda();

            // usa StatusStrip padrão da BaseForm
            lblStatus = BaseStatusLabel;

            Controls.AddRange(new Control[] {
                lblCli, cmbCliente, lblVeic, cmbVeiculo,
                lblItem, txtItem, lblQtd, txtQtd, lblVu, txtValorUnit, btnAdicionar,
                gridItens, btnRemoverItem, lblTotal, txtTotal,
                lblForma, cmbForma, btnSalvar, btnNovo,
                lblHist, gridVendas, btnExcluir, btnRecibo
            });
        }

        private DataGridView NovoGrid()
        {
            return new DataGridView
            {
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private void CarregarClientes()
        {
            cmbCliente.Items.Clear();
            foreach (Cliente c in ClienteDAO.Listar(""))
            {
                cmbCliente.Items.Add(new ComboCliente { Id = c.Id, Nome = c.NomeRazao });
            }
            if (cmbCliente.Items.Count > 0) cmbCliente.SelectedIndex = 0;
        }

        private void CarregarVeiculos()
        {
            cmbVeiculo.Items.Clear();
            var dono = cmbCliente.SelectedItem as ComboCliente;
            if (dono == null) return;
            foreach (Veiculo v in VeiculoDAO.Listar(dono.Id))
            {
                cmbVeiculo.Items.Add(new ComboVeiculo { Veiculo = v });
            }
            if (cmbVeiculo.Items.Count > 0) cmbVeiculo.SelectedIndex = 0;
        }

        private void AdicionarItem()
        {
            if (string.IsNullOrWhiteSpace(txtItem.Text)) { MessageBox.Show("Informe o item ou serviço.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            double qtd = 1;
            double.TryParse(txtQtd.Text, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out qtd);
            double unit = 0;
            double.TryParse(txtValorUnit.Text, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out unit);

            itensAtuais.Add(new VendaItem { Descricao = txtItem.Text.Trim(), Quantidade = qtd, ValorUnit = unit });
            AtualizarGridItens();

            txtItem.Clear();
            txtValorUnit.Clear();
            txtQtd.Text = "1";
            txtItem.Focus();
        }

        private void RemoverItem()
        {
            if (gridItens.SelectedRows.Count == 0) return;
            int idx = gridItens.SelectedRows[0].Index;
            if (idx >= 0 && idx < itensAtuais.Count) itensAtuais.RemoveAt(idx);
            AtualizarGridItens();
        }

        private void AtualizarGridItens()
        {
            gridItens.Rows.Clear();
            double total = 0;
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            foreach (var item in itensAtuais)
            {
                double sub = item.Quantidade * item.ValorUnit;
                total += sub;
                gridItens.Rows.Add(item.Descricao, item.Quantidade.ToString("0.##", cult), item.ValorUnit.ToString("N2", cult), sub.ToString("N2", cult));
            }
            txtTotal.Text = total.ToString("N2", cult);
        }

        private void SalvarVenda()
        {
            var dono = cmbCliente.SelectedItem as ComboCliente;
            if (dono == null) { MessageBox.Show("Selecione o cliente.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (itensAtuais.Count == 0) { MessageBox.Show("Adicione pelo menos um item à venda.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            double total = 0;
            foreach (var i in itensAtuais) total += i.Quantidade * i.ValorUnit;

            try
            {
                var venda = new Venda
                {
                    Id = _vendaId ?? 0,
                    Data = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    ClienteId = dono.Id,
                    VeiculoId = (cmbVeiculo.SelectedItem as ComboVeiculo)?.Veiculo.Id,
                    ValorTotal = total,
                    FormaPagamento = cmbForma.SelectedItem?.ToString() ?? "",
                    Itens = new List<VendaItem>(itensAtuais)
                };

                VendaDAO.SalvarComCaixa(venda);
                MessageBox.Show("Venda registrada com sucesso!", "SOEN", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LimparFormulario();
                CarregarVendas();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                MessageBox.Show("Erro ao salvar. Veja o log para detalhes.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CarregarVendaParaEdicao()
        {
            if (gridVendas.SelectedRows.Count == 0) return;
            var id = Convert.ToInt64(gridVendas.SelectedRows[0].Cells["Id"].Value);
            Venda v = VendaDAO.BuscarPorId(id);
            if (v == null) return;

            _vendaId = v.Id;
            SelecionarClientePorId(v.ClienteId);
            SelecionarVeiculoPorId(v.VeiculoId);
            cmbForma.SelectedItem = v.FormaPagamento;

            itensAtuais.Clear();
            foreach (var item in v.Itens) itensAtuais.Add(item);
            AtualizarGridItens();
            lblStatus.Text = "Editando venda nº " + v.Id + ". Ao salvar, o lançamento do caixa não é duplicado.";
        }

        private void SelecionarClientePorId(long? id)
        {
            if (!id.HasValue) return;
            for (int i = 0; i < cmbCliente.Items.Count; i++)
            {
                var c = cmbCliente.Items[i] as ComboCliente;
                if (c != null && c.Id == id.Value) { cmbCliente.SelectedIndex = i; return; }
            }
        }

        private void SelecionarVeiculoPorId(long? id)
        {
            if (!id.HasValue) return;
            for (int i = 0; i < cmbVeiculo.Items.Count; i++)
            {
                var v = cmbVeiculo.Items[i] as ComboVeiculo;
                if (v != null && v.Veiculo.Id == id.Value) { cmbVeiculo.SelectedIndex = i; return; }
            }
        }

        private void ImprimirReciboVenda()
        {
            if (gridVendas.SelectedRows.Count == 0) return;
            var id = Convert.ToInt64(gridVendas.SelectedRows[0].Cells["Id"].Value);
            Venda v = VendaDAO.BuscarPorId(id);
            if (v == null) return;
            Cliente c = v.ClienteId.HasValue ? ClienteDAO.BuscarPorId(v.ClienteId.Value) : null;
            RelatorioHelper.VisualizarReciboVenda(v, c);
        }

        private void ExcluirVenda()
        {
            if (gridVendas.SelectedRows.Count == 0) return;
            var id = Convert.ToInt64(gridVendas.SelectedRows[0].Cells["Id"].Value);
            if (MessageBox.Show("Excluir a venda nº " + id + "?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                VendaDAO.Excluir(id);
                CarregarVendas();
            }
        }

        private void CarregarVendas()
        {
            gridVendas.Rows.Clear();
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            foreach (Venda v in VendaDAO.Listar(null))
            {
                gridVendas.Rows.Add(v.Id, v.Data, v.NomeCliente, v.VeiculoDesc, v.ValorTotal.ToString("N2", cult), v.FormaPagamento);
            }
            lblStatus.Text = VendaDAO.Listar(null).Count + " venda(s) registrada(s).";
        }

        private void LimparFormulario()
        {
            _vendaId = null;
            itensAtuais.Clear();
            AtualizarGridItens();
            cmbForma.SelectedIndex = 0;
            lblStatus.Text = "";
        }
    }
}