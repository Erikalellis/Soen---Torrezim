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
    /// Criação de Orçamentos / Notas de Serviço com múltiplos itens
    /// (descrição + quantidade + valor unitário por linha).
    /// </summary>
    public partial class CriacaoOrcamentos : BaseForm
    {
        private ComboBox cmbCliente;
        private ComboBox cmbVeiculo;
        private ComboBox cmbServico;
        private ComboBox cmbTipo;
        private ComboBox cmbTecnico;
        private ComboBox cmbPeca;
        private TextBox txtItem;
        private TextBox txtQtd;
        private TextBox txtValorUnit;
        private Button btnAdicionar;
        private Button btnGerar;
        private Button btnEditar;
        private Button btnAprovar;
        private Button btnConverter;
        private Button btnExcluir;
        private Button btnVisualizar;
        private Label lblAviso;
        private DataGridView gridItens;
        private DataGridView grid;
        private Label lblTotal;
        private ToolStripStatusLabel lblStatus;

        private readonly List<OrcamentoItem> itensAtuais = new List<OrcamentoItem>();
        private long? _editandoId;
        private string _editandoData;
        private string _editandoNumero;

        public CriacaoOrcamentos()
        {
            InitializeComponent();
            Text = "Soen - Orçamentos e Notas de Serviço";
            ClientSize = new Size(900, 660);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;

            CriarInterface();
            CarregarClientes();
            CarregarServicos();
            CarregarTecnicos();
            CarregarPecas();
            CarregarOrcamentos();
        }

        private void CriarInterface()
        {
            var l1 = new Label { Text = "Cliente:", AutoSize = true, Location = new Point(12, 20) };
            cmbCliente = new ComboBox { Location = new Point(70, 16), Size = new Size(240, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCliente.SelectedIndexChanged += (s, e) => CarregarVeiculos();

            var l2 = new Label { Text = "Veículo:", AutoSize = true, Location = new Point(325, 20) };
            cmbVeiculo = new ComboBox { Location = new Point(385, 16), Size = new Size(170, 21), DropDownStyle = ComboBoxStyle.DropDownList };

            var l3 = new Label { Text = "Serviço catálogo:", AutoSize = true, Location = new Point(575, 17) };
            cmbServico = new ComboBox { Location = new Point(690, 16), Size = new Size(190, 21), DropDownStyle = ComboBoxStyle.DropDown };

            var l4 = new Label { Text = "Tipo:", AutoSize = true, Location = new Point(12, 52) };
            cmbTipo = new ComboBox { Location = new Point(60, 49), Size = new Size(230, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTipo.Items.Add("Orçamento (sem compromisso)");
            cmbTipo.Items.Add("Nota de Serviço / OS");
            cmbTipo.SelectedIndex = 0;

            var lT = new Label { Text = "Técnico:", AutoSize = true, Location = new Point(325, 52) };
            cmbTecnico = new ComboBox { Location = new Point(395, 49), Size = new Size(170, 21), DropDownStyle = ComboBoxStyle.DropDownList };

            var lPe = new Label { Text = "Peça/estoque:", AutoSize = true, Location = new Point(585, 52) };
            cmbPeca = new ComboBox { Location = new Point(685, 49), Size = new Size(190, 21), DropDownStyle = ComboBoxStyle.DropDownList };

            var l5 = new Label { Text = "Serviço/item:", AutoSize = true, Location = new Point(12, 90) };
            txtItem = new TextBox { Location = new Point(95, 87), Size = new Size(430, 20) };
            var l6 = new Label { Text = "Qtd:", AutoSize = true, Location = new Point(535, 90) };
            txtQtd = new TextBox { Location = new Point(570, 87), Size = new Size(50, 20), Text = "1" };
            var l7 = new Label { Text = "R$ unit:", AutoSize = true, Location = new Point(630, 90) };
            txtValorUnit = new TextBox { Location = new Point(685, 87), Size = new Size(80, 20) };
            btnAdicionar = UIHelpers.CreateButton("Adicionar", new Point(775, 85), new Size(95, 26));
            btnAdicionar.Click += (s, e) => AdicionarItem();

            gridItens = new DataGridView
            {
                Location = new Point(12, 120),
                Size = new Size(860, 160),
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            gridItens.Columns.Add("Descricao", "Serviço / Item");
            gridItens.Columns.Add("Qtd", "Qtd");
            gridItens.Columns.Add("Unit", "Valor Unit");
            gridItens.Columns.Add("Tot", "Total");
            gridItens.Columns["Descricao"].FillWeight = 4f;

            lblTotal = new Label { Text = "Total: R$ 0,00", AutoSize = true, Location = new Point(12, 289), Font = new Font("Segoe UI", 11, FontStyle.Bold) };

            btnGerar = UIHelpers.CreateButton("Emitir / Salvar", new Point(395, 317), new Size(120, 28));
            btnGerar.Click += (s, e) => SalvarOrcamento("em_aberto");
            btnEditar = UIHelpers.CreateButton("Editar Sel.", new Point(523, 317), new Size(110, 28));
            btnEditar.Click += (s, e) => CarregarEdicao();
            btnAprovar = UIHelpers.CreateButton("Aprovar Sel.", new Point(641, 317), new Size(110, 28));
            btnAprovar.Click += (s, e) => AlterarStatus("aprovado");
            btnConverter = UIHelpers.CreateButton("Converter", new Point(759, 317), new Size(95, 28));
            btnConverter.Click += (s, e) => ConverterSel();
            btnExcluir = UIHelpers.CreateButton("Excluir Sel.", new Point(13, 350), new Size(110, 28));
            btnExcluir.Click += (s, e) => Excluir();
            btnVisualizar = UIHelpers.CreateButton("Visualizar Doc.", new Point(131, 350), new Size(120, 28));
            btnVisualizar.Click += (s, e) => VisualizarDocumento();

            lblAviso = new Label { Text = "", AutoSize = true, Location = new Point(262, 354), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.DarkBlue };

            grid = new DataGridView
            {
                Location = new Point(12, 392),
                Size = new Size(860, 230),
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            grid.Columns.Add("Id", "Nº");
            grid.Columns.Add("Numero", "Nota/OS");
            grid.Columns.Add("Data", "Data");
            grid.Columns.Add("Cliente", "Cliente");
            grid.Columns.Add("Veiculo", "Veículo");
            grid.Columns.Add("Valor", "Valor");
            grid.Columns.Add("Status", "Status");
            grid.Columns.Add("Tipo", "Tipo");
            grid.Columns["Cliente"].FillWeight = 2f;

            // usa StatusStrip padrão da BaseForm
            lblStatus = BaseStatusLabel;

            Controls.AddRange(new Control[] { l1, cmbCliente, l2, cmbVeiculo, l3, cmbServico, l4, cmbTipo,
                lT, cmbTecnico, lPe, cmbPeca,
                l5, txtItem, l6, txtQtd, l7, txtValorUnit, btnAdicionar, gridItens, lblTotal,
                btnGerar, btnEditar, btnAprovar, btnConverter, btnExcluir, btnVisualizar, lblAviso,
                grid });
            // BaseForm já adicionou o StatusStrip ao Controls no construtor.

            txtValorUnit.TextChanged += (s, e) => Validacoes.AplicarMascaraValor(txtValorUnit);
            cmbServico.SelectedIndexChanged += (s, e) => PreencherItemDoCatalogo();
        }

        private void CarregarClientes()
        {
            cmbCliente.Items.Clear();
            foreach (Cliente c in ClienteDAO.Listar(""))
                cmbCliente.Items.Add(new ComboCliente { Id = c.Id, Nome = c.NomeRazao });
            if (cmbCliente.Items.Count > 0) cmbCliente.SelectedIndex = 0;
        }

        private void CarregarVeiculos()
        {
            cmbVeiculo.Items.Clear();
            var dono = cmbCliente.SelectedItem as ComboCliente;
            if (dono == null) return;
            foreach (Veiculo v in VeiculoDAO.Listar(dono.Id))
                cmbVeiculo.Items.Add(new ComboVeiculo { Veiculo = v });
            if (cmbVeiculo.Items.Count > 0) cmbVeiculo.SelectedIndex = 0;
        }

        private void CarregarServicos()
        {
            cmbServico.Items.Clear();
            cmbServico.Items.Add("(selecione o serviço)");
            foreach (Servico s in ServicoDAO.ListarServicos())
                cmbServico.Items.Add(s.Nome);
            cmbServico.SelectedIndex = 0;
        }

        private void CarregarTecnicos()
        {
            cmbTecnico.Items.Clear();
            cmbTecnico.Items.Add("(sem técnico)");
            foreach (Tecnico t in TecnicoDAO.Listar(somenteAtivos: true))
                cmbTecnico.Items.Add(new ComboTecnico { Id = t.Id, Nome = t.Nome, Comissao = t.ComissaoPercent });
            cmbTecnico.SelectedIndex = 0;
        }

        private void CarregarPecas()
        {
            cmbPeca.Items.Clear();
            cmbPeca.Items.Add("(sem peça/estoque)");
            foreach (Produto p in ProdutoDAO.Listar())
                cmbPeca.Items.Add(new ComboProduto { Id = p.Id, Nome = p.Nome, Qtd = p.QtdAtual });
            cmbPeca.SelectedIndex = 0;
        }

        private void PreencherItemDoCatalogo()
        {
            if (cmbServico.SelectedIndex > 0)
                txtItem.Text = cmbServico.SelectedItem.ToString();
        }

        private void AdicionarItem()
        {
            if (string.IsNullOrWhiteSpace(txtItem.Text))
            {
                MessageBox.Show("Informe a descrição do serviço/item.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            double qtd = 1;
            double.TryParse(txtQtd.Text, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out qtd);
            double vu = (double)Validacoes.LerValor(txtValorUnit);
            var peca = cmbPeca.SelectedItem as ComboProduto;
            itensAtuais.Add(new OrcamentoItem
            {
                Descricao = txtItem.Text.Trim(),
                Quantidade = qtd,
                ValorUnit = vu,
                ProdutoId = peca != null ? peca.Id : (long?)null
            });
            CarregarItens();
            txtItem.Clear();
            txtQtd.Text = "1";
            txtValorUnit.Clear();
            cmbPeca.SelectedIndex = 0;
            txtItem.Focus();
        }

        private void CarregarItens()
        {
            gridItens.Rows.Clear();
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            foreach (OrcamentoItem it in itensAtuais)
                gridItens.Rows.Add(it.Descricao, it.Quantidade.ToString("0.##", cult), it.ValorUnit.ToString("N2", cult), it.ValorTotal.ToString("N2", cult));
            double total = 0;
            foreach (OrcamentoItem it in itensAtuais) total += it.ValorTotal;
            lblTotal.Text = "Total: R$ " + total.ToString("N2", cult);
        }

        private void SalvarOrcamento(string status)
        {
            var dono = cmbCliente.SelectedItem as ComboCliente;
            if (dono == null) { MessageBox.Show("Selecione o cliente.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (itensAtuais.Count == 0)
            {
                MessageBox.Show("Adicione ao menos um item/serviço.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                double total = 0;
                foreach (OrcamentoItem it in itensAtuais) total += it.ValorTotal;

                bool editando = _editandoId.HasValue;
                var tec = cmbTecnico.SelectedIndex > 0 ? (ComboTecnico)cmbTecnico.SelectedItem : null;
                var o = new Orcamento
                {
                    Id = _editandoId ?? 0,
                    Data = editando ? _editandoData : DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    ClienteId = dono.Id,
                    VeiculoId = (cmbVeiculo.SelectedItem as ComboVeiculo)?.Veiculo.Id,
                    Servico = itensAtuais[0].Descricao + (itensAtuais.Count > 1 ? " (+" + (itensAtuais.Count - 1) + " item(ns))" : ""),
                    Valor = total,
                    Status = status,
                    Tipo = cmbTipo.SelectedIndex == 1 ? "nota" : "orcamento",
                    Numero = editando ? _editandoNumero : null,
                    TecnicoId = tec != null ? tec.Id : (long?)null,
                    Comissao = tec != null ? total * (tec.Comissao / 100.0) : 0
                };
                ServicoDAO.SalvarOrcamento(o);
                ServicoDAO.SalvarItensOrcamento(o.Id, itensAtuais);

                string numero = o.Numero;
                bool notaNova = !editando && o.Tipo == "nota";
                _editandoId = null;
                _editandoData = null;
                _editandoNumero = null;
                LimparEdicao();
                CarregarOrcamentos();

                MessageBox.Show(notaNova
                    ? "Nota de Serviço emitida! Número: " + numero
                    : (editando ? "Alterações salvas com sucesso!" : "Orçamento gerado!"),
                    "SOEN", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                MessageBox.Show("Erro ao processar. Veja o log para detalhes.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CarregarEdicao()
        {
            var id = Selecionado();
            if (!id.HasValue) return;
            Orcamento o = null;
            foreach (Orcamento item in ServicoDAO.ListarOrcamentos())
                if (item.Id == id.Value) { o = item; break; }
            if (o == null) return;

            _editandoId = o.Id;
            _editandoData = o.Data;
            _editandoNumero = o.Numero;
            // seleciona cliente
            for (int i = 0; i < cmbCliente.Items.Count; i++)
            {
                var cc = cmbCliente.Items[i] as ComboCliente;
                if (cc != null && cc.Id == o.ClienteId) { cmbCliente.SelectedIndex = i; break; }
            }
            // seleciona veículo correspondente
            if (o.VeiculoId.HasValue)
                for (int i = 0; i < cmbVeiculo.Items.Count; i++)
                    if (((ComboVeiculo)cmbVeiculo.Items[i]).Veiculo.Id == o.VeiculoId.Value) { cmbVeiculo.SelectedIndex = i; break; }
            // seleciona técnico
            if (o.TecnicoId.HasValue)
                for (int i = 0; i < cmbTecnico.Items.Count; i++)
                    if (cmbTecnico.Items[i] is ComboTecnico && ((ComboTecnico)cmbTecnico.Items[i]).Id == o.TecnicoId.Value) { cmbTecnico.SelectedIndex = i; break; }
            // itens
            itensAtuais.Clear();
            itensAtuais.AddRange(ServicoDAO.ListarItensOrcamento(o.Id));
            cmbTipo.SelectedIndex = o.Tipo == "nota" ? 1 : 0;
            CarregarItens();
            lblAviso.Text = "Editando: " + (string.IsNullOrWhiteSpace(o.Numero) ? "nº " + o.Id : o.Numero) + " — clique em 'Emitir / Salvar' para gravar as alterações.";
        }

        private void LimparEdicao()
        {
            lblAviso.Text = "";
            itensAtuais.Clear();
            CarregarItens();
            cmbTipo.SelectedIndex = 0;
            cmbServico.SelectedIndex = 0;
        }

        private void CarregarOrcamentos()
        {
            grid.Rows.Clear();
            List<Orcamento> lista = ServicoDAO.ListarOrcamentos();
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            foreach (var o in lista)
            {
                string numero = string.IsNullOrWhiteSpace(o.Numero) ? "" : o.Numero;
                string tipo = o.Tipo == "nota" ? "Nota de Serviço" : "Orçamento";
                grid.Rows.Add(o.Id, numero, o.Data, o.NomeCliente, o.VeiculoPlaca, o.Valor.ToString("N2", cult), o.Status, tipo);
            }
            int qtdNotas = 0;
            foreach (var o in lista) if (o.Tipo == "nota") qtdNotas++;
            lblStatus.Text = lista.Count + " registro(s) — Notas de Serviço: " + qtdNotas + ".";
        }

        private void AlterarStatus(string status)
        {
            var id = Selecionado();
            if (!id.HasValue) return;
            List<Orcamento> lista = ServicoDAO.ListarOrcamentos();
            foreach (var o in lista)
            {
                if (o.Id == id.Value)
                {
                    o.Status = status;
                    o.Id = id.Value;
                    ServicoDAO.SalvarOrcamento(o);
                    CarregarOrcamentos();
                    return;
                }
            }
        }

        private void ConverterSel()
        {
            var id = Selecionado();
            if (!id.HasValue) return;
            Orcamento o = null;
            foreach (Orcamento item in ServicoDAO.ListarOrcamentos())
                if (item.Id == id.Value) { o = item; break; }
            if (o == null) return;

            if (o.Status != "aprovado")
            {
                MessageBox.Show("Aprove a OS antes de converter em venda.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Converter a OS " + (string.IsNullOrWhiteSpace(o.Numero) ? "nº " + o.Id : o.Numero) +
                " em venda? A baixa de estoque das peças vinculadas será realizada.",
                "Converter em venda", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                ServicoDAO.ConverterEmVenda(id.Value);
                CarregarOrcamentos();
                MessageBox.Show("OS convertida em venda! Caixa e estoque atualizados.", "SOEN",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Não foi possível converter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                MessageBox.Show("Erro ao converter. Veja o log para detalhes.", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private long? Selecionado()
        {
            if (grid.SelectedRows.Count == 0) return null;
            return Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
        }

        private void Excluir()
        {
            var id = Selecionado();
            if (!id.HasValue) return;
            if (MessageBox.Show("Excluir este orçamento?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ServicoDAO.ExcluirOrcamento(id.Value);
                CarregarOrcamentos();
            }
        }

        /// <summary>Pré-preenche o formulário a partir de um agendamento concluído.</summary>
        public void PreencherParaAgendamento(long? clienteId, long? veiculoId, string descricao, double valor)
        {
            if (clienteId.HasValue)
                for (int i = 0; i < cmbCliente.Items.Count; i++)
                    if (((ComboCliente)cmbCliente.Items[i]).Id == clienteId.Value) { cmbCliente.SelectedIndex = i; break; }
            if (veiculoId.HasValue)
                for (int i = 0; i < cmbVeiculo.Items.Count; i++)
                    if (((ComboVeiculo)cmbVeiculo.Items[i]).Veiculo.Id == veiculoId.Value) { cmbVeiculo.SelectedIndex = i; break; }
            if (!string.IsNullOrWhiteSpace(descricao))
                itensAtuais.Add(new OrcamentoItem { Descricao = descricao, Quantidade = 1, ValorUnit = valor });
            CarregarItens();
        }

        private void VisualizarDocumento()
        {
            var id = Selecionado();
            if (!id.HasValue) return;
            Orcamento o = null;
            foreach (Orcamento item in ServicoDAO.ListarOrcamentos())
                if (item.Id == id.Value) { o = item; break; }
            if (o == null) return;

            Cliente c = o.ClienteId.HasValue ? ClienteDAO.BuscarPorId(o.ClienteId.Value) : null;
            Veiculo v = o.VeiculoId.HasValue ? VeiculoDAO.BuscarPorId(o.VeiculoId.Value) : null;
            o.Itens = ServicoDAO.ListarItensOrcamento(o.Id);
            RelatorioHelper.VisualizarDocumento(o, c, v);
        }
    }
}