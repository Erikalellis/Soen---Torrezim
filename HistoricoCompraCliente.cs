using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>Histórico de Compras do Cliente: consulta o cliente e mostra as vendas dele.</summary>
    public partial class HistoricoCompraCliente : Form
    {
        private TextBox txtBuscar;
        private Button btnBuscar;
        private ComboBox cmbCliente;
        private DataGridView grid;
        private Label lblTotal;

        public HistoricoCompraCliente()
        {
            InitializeComponent();
            Text = "Soen - Histórico de Compras do Cliente";
            ClientSize = new Size(760, 480);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;
            CriarInterface();
            CarregarClientes("");
        }

        private void CriarInterface()
        {
            var l1 = new Label { Text = "Buscar cliente:", AutoSize = true, Location = new Point(12, 20) };
            txtBuscar = new TextBox { Location = new Point(110, 17), Size = new Size(200, 20) };
            btnBuscar = UIHelpers.CreateButton("Buscar", new Point(320, 15), new Size(80, 28));
            btnBuscar.Click += (s, e) => CarregarClientes(txtBuscar.Text);

            var l2 = new Label { Text = "Cliente:", AutoSize = true, Location = new Point(12, 55) };
            cmbCliente = new ComboBox { Location = new Point(70, 51), Size = new Size(400, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCliente.SelectedIndexChanged += (s, e) => Carregar();

            lblTotal = new Label { Text = "", AutoSize = true, Location = new Point(12, 85), Font = new Font("Segoe UI", 9, FontStyle.Bold) };

            grid = new DataGridView
            {
                Location = new Point(12, 110),
                Size = new Size(730, 340),
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            grid.Columns.Add("Id", "Nº Venda");
            grid.Columns.Add("Data", "Data");
            grid.Columns.Add("Valor", "Valor");
            grid.Columns.Add("Forma", "Pagamento");

            Controls.AddRange(new Control[] { l1, txtBuscar, btnBuscar, l2, cmbCliente, lblTotal, grid });
        }

        private void CarregarClientes(string filtro)
        {
            cmbCliente.Items.Clear();
            foreach (Cliente c in ClienteDAO.Listar(filtro))
                if (c.Tipo == "cliente")
                    cmbCliente.Items.Add(new ComboCliente { Id = c.Id, Nome = c.NomeRazao });
            if (cmbCliente.Items.Count > 0) cmbCliente.SelectedIndex = 0;
            Carregar();
        }

        private void Carregar()
        {
            grid.Rows.Clear();
            var dono = cmbCliente.SelectedItem as ComboCliente;
            if (dono == null) { lblTotal.Text = "Selecione um cliente."; return; }
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            double total = 0;
            foreach (Venda v in VendaDAO.Listar(dono.Id))
            {
                total += v.ValorTotal;
                grid.Rows.Add(v.Id, v.Data, v.ValorTotal.ToString("N2", cult), v.FormaPagamento);
            }
            lblTotal.Text = "Total comprado por " + dono.Nome + ": " + total.ToString("N2", cult);
        }
    }
}