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
    /// Cadastro de Técnicos da oficina. Os técnicos podem ser vinculados
    /// a Orçamentos/Notas de Serviço para controle de comissão.
    /// </summary>
    public partial class Tecnicos : BaseForm
    {
        private TextBox txtNome;
        private TextBox txtTelefone;
        private TextBox txtCargo;
        private TextBox txtComissao;
        private CheckBox chkAtivo;
        private Button btnSalvar;
        private Button btnNovo;
        private Button btnExcluir;
        private DataGridView grid;
        private ToolStripStatusLabel lblStatus;

        private long _editandoId;

        public Tecnicos()
        {
            InitializeComponent();
            Text = "Soen - Técnicos";
            ClientSize = new Size(700, 520);
            CriarInterface();
            Carregar();
        }

        private void CriarInterface()
        {
            var l1 = new Label { Text = "Nome:", AutoSize = true, Location = new Point(12, 20) };
            txtNome = new TextBox { Location = new Point(70, 17), Size = new Size(290, 20) };

            var l2 = new Label { Text = "Telefone:", AutoSize = true, Location = new Point(375, 20) };
            txtTelefone = new TextBox { Location = new Point(445, 17), Size = new Size(130, 20) };

            var l3 = new Label { Text = "Cargo:", AutoSize = true, Location = new Point(12, 55) };
            txtCargo = new TextBox { Location = new Point(70, 52), Size = new Size(290, 20) };

            var l4 = new Label { Text = "Comissão %:", AutoSize = true, Location = new Point(375, 55) };
            txtComissao = new TextBox { Location = new Point(445, 52), Size = new Size(70, 20) };

            chkAtivo = new CheckBox { Text = "Técnico ativo", Location = new Point(535, 53), AutoSize = true, Checked = true };

            btnSalvar = UIHelpers.CreateButton("Salvar", new Point(12, 90), new Size(110, 28));
            btnSalvar.Click += (s, e) => Salvar();
            btnNovo = UIHelpers.CreateButton("Novo", new Point(130, 90), new Size(110, 28));
            btnNovo.Click += (s, e) => LimparCampos();
            btnExcluir = UIHelpers.CreateButton("Excluir Sel.", new Point(248, 90), new Size(110, 28));
            btnExcluir.Click += (s, e) => Excluir();

            grid = new DataGridView
            {
                Location = new Point(12, 130),
                Size = new Size(670, 330),
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White
            };
            grid.Columns.Add("Id", "Id");
            grid.Columns.Add("Nome", "Nome");
            grid.Columns.Add("Telefone", "Telefone");
            grid.Columns.Add("Cargo", "Cargo");
            grid.Columns.Add("Comissao", "Comissão %");
            grid.Columns.Add("Ativo", "Ativo");
            grid.Columns["Id"].FillWeight = 0.5f;
            grid.Columns["Nome"].FillWeight = 2.2f;
            grid.CellDoubleClick += (s, e) => CarregarEdicao();

            lblStatus = BaseStatusLabel;

            Controls.AddRange(new Control[] { l1, txtNome, l2, txtTelefone, l3, txtCargo, l4, txtComissao,
                chkAtivo, btnSalvar, btnNovo, btnExcluir, grid });

            txtTelefone.TextChanged += (s, e) => Validacoes.AplicarMascaraTelefone(txtTelefone);
            txtComissao.TextChanged += (s, e) => Validacoes.AplicarMascaraValor(txtComissao);
        }

        private void Carregar()
        {
            grid.Rows.Clear();
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            foreach (Tecnico t in TecnicoDAO.Listar())
                grid.Rows.Add(t.Id, t.Nome, t.Telefone, t.Cargo, t.ComissaoPercent.ToString("0.##", cult), t.Ativo ? "Sim" : "Não");
            lblStatus.Text = TecnicoDAO.Listar().Count + " técnico(s) cadastrado(s).";
        }

        private void Salvar()
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show("Informe o nome do técnico.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            double percent = 0;
            double.TryParse(txtComissao.Text, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out percent);
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;

            try
            {
                var t = new Tecnico
                {
                    Id = _editandoId,
                    Nome = txtNome.Text.Trim(),
                    Telefone = txtTelefone.Text.Trim(),
                    Cargo = txtCargo.Text.Trim(),
                    ComissaoPercent = percent,
                    Ativo = chkAtivo.Checked
                };
                TecnicoDAO.Salvar(t);
                _editandoId = 0;
                LimparCampos();
                Carregar();
                MessageBox.Show("Técnico salvo!", "SOEN", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                MessageBox.Show("Erro ao salvar. Veja o log para detalhes.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CarregarEdicao()
        {
            if (grid.SelectedRows.Count == 0) return;
            long id = Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
            foreach (Tecnico t in TecnicoDAO.Listar())
            {
                if (t.Id == id)
                {
                    _editandoId = t.Id;
                    txtNome.Text = t.Nome;
                    txtTelefone.Text = t.Telefone;
                    txtCargo.Text = t.Cargo;
                    txtComissao.Text = t.ComissaoPercent.ToString("0.##", CultureInfo.GetCultureInfo("pt-BR"));
                    chkAtivo.Checked = t.Ativo;
                    lblStatus.Text = "Editando: " + t.Nome + " — clique em 'Salvar' para gravar.";
                    return;
                }
            }
        }

        private void LimparCampos()
        {
            _editandoId = 0;
            txtNome.Clear();
            txtTelefone.Clear();
            txtCargo.Clear();
            txtComissao.Clear();
            chkAtivo.Checked = true;
            lblStatus.Text = "";
            txtNome.Focus();
        }

        private void Excluir()
        {
            if (!Sessao.PrepararExclusao()) return;
            if (grid.SelectedRows.Count == 0) return;
            long id = Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
            if (MessageBox.Show("Excluir este técnico?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                TecnicoDAO.Excluir(id);
                if (_editandoId == id) _editandoId = 0;
                LimparCampos();
                Carregar();
            }
        }
    }
}
