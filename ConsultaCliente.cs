using System;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>
    /// Consulta e gestão de clientes: busca, lista, edita e exclui.
    /// Mostra um DataGridView com os cadastros e ações para cada registro.
    /// </summary>
    public partial class ConsultaCliente : BaseForm
    {
        private DataGridView grid;
        private TextBox txtBusca;
        private Button btnBuscar;
        private Button btnTodos;
        private Button btnNovo;
        private Button btnEditar;
        private Button btnExcluir;
        private Button btnHistorico;
        private StatusStrip statusBar;
        private ToolStripStatusLabel lblStatus;

        public ConsultaCliente()
        {
            InitializeComponent();
            Text = "Soen - Consulta de Clientes";
            ClientSize = new Size(860, 460);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;

            CriarInterface();
            CarregarClientes("");
        }

        private void CriarInterface()
        {
            // Filtro
            txtBusca = new TextBox { Location = new Point(12, 18), Size = new Size(300, 22) };
            txtBusca.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) CarregarClientes(txtBusca.Text.Trim()); };

            btnBuscar = CriarBotao("Buscar", 320, 14, (s, e) => CarregarClientes(txtBusca.Text.Trim()));
            btnTodos = CriarBotao("Mostrar Todos", 420, 14, (s, e) => { txtBusca.Clear(); CarregarClientes(""); });

            // Ações
            btnNovo = CriarBotao("Novo Cliente", 12, 50, (s, e) => AbrirNovo());
            btnEditar = CriarBotao("Editar", 110, 50, (s, e) => AbrirEdicao());
            btnExcluir = CriarBotao("Excluir", 205, 50, (s, e) => Excluir());
            btnHistorico = CriarBotao("Histórico", 310, 50, (s, e) => AbrirHistorico());

            // Grid
            grid = new DataGridView
            {
                Location = new Point(12, 88),
                Size = new Size(830, 330),
                ReadOnly = true,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false
            };

            grid.Columns.Add("Id", "Código");
            grid.Columns.Add("CpfCnpj", "CPF/CNPJ");
            grid.Columns.Add("Nome", "Nome / Razão Social");
            grid.Columns.Add("Fone1", "Telefone");
            grid.Columns.Add("Cidade", "Cidade");
            grid.Columns.Add("Estado", "UF");
            grid.Columns.Add("Email1", "E-mail");
            grid.Columns["Nome"].FillWeight = 4f;
            grid.Columns["Cidade"].FillWeight = 2f;
            grid.Columns["Email1"].FillWeight = 3f;

            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) AbrirEdicao(); };

            statusBar = new StatusStrip { Location = new Point(0, 418) };
            lblStatus = new ToolStripStatusLabel(" ");
            statusBar.Items.Add(lblStatus);

            Controls.Add(txtBusca);
            Controls.Add(btnBuscar);
            Controls.Add(btnTodos);
            Controls.Add(btnNovo);
            Controls.Add(btnEditar);
            Controls.Add(btnExcluir);
            Controls.Add(btnHistorico);
            Controls.Add(grid);
            Controls.Add(statusBar);
        }

        private Button CriarBotao(string texto, int x, int y, EventHandler clique)
        {
            var b = new Button
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(100, 26),
                BackColor = SystemColors.AppWorkspace
            };
            b.Click += clique;
            return b;
        }

        private void CarregarClientes(string filtro)
        {
            var lista = ClienteDAO.Listar(filtro);
            grid.Rows.Clear();

            foreach (var c in lista)
            {
                grid.Rows.Add(c.Id, c.CpfCnpj, c.NomeRazao, c.Fone1, c.Cidade, c.Estado, c.Email1);
            }
            lblStatus.Text = lista.Count + " cliente(s) encontrado(s).";
        }

        private void AbrirNovo()
        {
            new CadastroCliente().Show();
            CarregarClientes(txtBusca.Text.Trim());
        }

        private void AbrirEdicao()
        {
            var c = ClienteSelecionado();
            if (c == null)
            {
                MessageBox.Show("Selecione um cliente na lista.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            new CadastroCliente(c).ShowDialog();
            CarregarClientes(txtBusca.Text.Trim());
        }

        private void AbrirHistorico()
        {
            var c = ClienteSelecionado();
            if (c == null)
            {
                MessageBox.Show("Selecione um cliente na lista para ver o histórico.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            new HistoricoCliente(c).ShowDialog();
        }

        private void Excluir()
        {
            var c = ClienteSelecionado();
            if (c == null)
            {
                MessageBox.Show("Selecione um cliente na lista para excluir.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var conf = MessageBox.Show("Excluir o cliente \"" + c.NomeRazao + "\"?\nEssa ação não pode ser desfeita.",
                "Confirmar exclusão", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (conf == DialogResult.Yes)
            {
                ClienteDAO.Excluir(c.Id);
                CarregarClientes(txtBusca.Text.Trim());
            }
        }

        private Cliente ClienteSelecionado()
        {
            if (grid.SelectedRows.Count == 0) return null;
            var id = Convert.ToInt64(grid.SelectedRows[0].Cells["Id"].Value);
            return ClienteDAO.BuscarPorId(id);
        }
    }
}