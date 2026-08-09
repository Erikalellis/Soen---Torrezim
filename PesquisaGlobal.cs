using System;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>
    /// Pesquisa global por clientes, veículos, orçamentos/OS e vendas.
    /// Duplo clique abre o cadastro/documento correspondente.
    /// </summary>
    public class PesquisaGlobal : BaseForm
    {
        private TextBox txtBusca;
        private Button btnBuscar;
        private DataGridView grid;

        public PesquisaGlobal()
        {
            Text = "Pesquisa Global";
            ClientSize = new Size(820, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            KeyPreview = true;
            CriarInterface();
        }

        private void CriarInterface()
        {
            txtBusca = new TextBox { Location = new Point(12, 16), Size = new Size(600, 24), Font = new Font("Segoe UI", 10) };
            txtBusca.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Buscar(); };

            btnBuscar = UIHelpers.CreateButton("Pesquisar", new Point(626, 14), new Size(120, 30));
            btnBuscar.Click += (s, e) => Buscar();

            var dica = new Label
            {
                Text = "Digite nome, CPF/CNPJ, placa, modelo, nº da OS ou descrição de serviço.",
                AutoSize = true,
                Location = new Point(12, 48),
                ForeColor = Color.DimGray
            };

            grid = new DataGridView
            {
                Location = new Point(12, 72),
                Size = new Size(796, 430),
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            grid.Columns.Add("Tipo", "Resultado");
            grid.Columns.Add("Detalhe", "Detalhe");
            grid.Columns.Add("Acao", "O que vai abrir");
            grid.Columns.Add("TDtipo", "Tipo");
            grid.Columns["TDtipo"].Visible = false;

            grid.CellDoubleClick += Grid_CellDoubleClick;

            Controls.AddRange(new Control[] { txtBusca, btnBuscar, dica, grid });
        }

        private void Buscar()
        {
            string f = txtBusca.Text.Trim();
            grid.Rows.Clear();
            if (string.IsNullOrWhiteSpace(f))
            {
                MessageBox.Show("Digite algo para pesquisar.", "Pesquisa Global", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (Cliente c in ClienteDAO.Listar(f))
                grid.Rows.Add("Cliente", c.NomeRazao + " — CPF/CNPJ: " + c.CpfCnpj, "Abrir cadastro do cliente", "cliente;" + c.Id);

            foreach (Veiculo v in VeiculoDAO.ListarPorBusca(f))
                grid.Rows.Add("Veículo", v.Placa + " — " + v.Marca + " " + v.Modelo + (string.IsNullOrWhiteSpace(v.NomeCliente) ? "" : " (" + v.NomeCliente + ")"), "Abrir cadastro de veículos", "veiculo;" + v.Id);

            foreach (Orcamento o in ServicoDAO.ListarOrcamentosPesquisa(f))
                grid.Rows.Add("OS / Orçamento",
                    (string.IsNullOrWhiteSpace(o.Numero) ? "#" + o.Id : o.Numero) + " — " + o.NomeCliente + " — " + o.Servico,
                    "Visualizar documento", "os;" + o.Id);

            foreach (Venda v in VendaDAO.ListarPesquisa(f))
                grid.Rows.Add("Venda", "Nº " + v.Id + " (" + v.Data + ") — " + v.NomeCliente + " — R$ " + v.ValorTotal.ToString("N2"),
                    "Visualizar recibo", "venda;" + v.Id);

            if (grid.Rows.Count == 0)
                MessageBox.Show("Nenhum resultado encontrado.", "Pesquisa Global", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = grid.Rows[e.RowIndex];
            string tag = (row.Cells["TDtipo"].Value ?? "").ToString();
            if (string.IsNullOrWhiteSpace(tag)) return;
            string[] partes = tag.Split(';');
            string tipo = partes[0];
            long id = partes.Length > 1 ? long.Parse(partes[1]) : 0;

            try
            {
                switch (tipo)
                {
                    case "cliente":
                        var c = ClienteDAO.BuscarPorId(id);
                        if (c != null) new CadastroCliente(c).Show();
                        break;
                    case "veiculo":
                        var vv = VeiculoDAO.BuscarPorId(id);
                        if (vv != null) new CadastroVeiculo(vv).Show();
                        break;
                    case "os":
                        var o = ServicoDAO.BuscarOrcamentoPorId(id);
                        if (o != null)
                        {
                            var cli = o.ClienteId.HasValue ? ClienteDAO.BuscarPorId(o.ClienteId.Value) : null;
                            var vei = o.VeiculoId.HasValue ? VeiculoDAO.BuscarPorId(o.VeiculoId.Value) : null;
                            RelatorioHelper.VisualizarDocumento(o, cli, vei);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                MessageBox.Show("Não foi possível abrir. Veja o log.", "Pesquisa Global", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}