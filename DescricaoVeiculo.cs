using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Soen___Torrezim.Data;
using Soen___Torrezim.Models;

namespace Soen___Torrezim
{
    /// <summary>
    /// Descrição detalhada de cada veículo: lista todos os veículos com seus
    /// dados completos (placa, marca, modelo, cor, observações e dono).
    /// </summary>
    public partial class DescricaoVeiculo : Form
    {
        private ComboBox cmbVeiculo;
        private TextBox txtPlaca;
        private TextBox txtMarca;
        private TextBox txtModelo;
        private TextBox txtCor;
        private TextBox txtObservacoes;
        private TextBox txtCliente;
        private StatusStrip statusBar;
        private ToolStripStatusLabel lblStatus;

        public DescricaoVeiculo()
        {
            InitializeComponent();
            Text = "Soen - Descrição Detalhada dos Veículos";
            ClientSize = new Size(520, 400);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;

            CriarInterface();
            CarregarVeiculos();
        }

        private void CriarInterface()
        {
            var lblSel = new Label { Text = "Veículo:", AutoSize = true, Location = new Point(20, 20) };
            cmbVeiculo = new ComboBox { Location = new Point(90, 17), Size = new Size(390, 21), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbVeiculo.SelectedIndexChanged += (s, e) => ExibirDetalhes();

            var lblCliente = new Label { Text = "Cliente (dono):", AutoSize = true, Location = new Point(20, 60) };
            txtCliente = new TextBox { Location = new Point(140, 56), Size = new Size(340, 20), ReadOnly = true, BackColor = Color.White };

            var lblPlaca = new Label { Text = "Placa:", AutoSize = true, Location = new Point(20, 90) };
            txtPlaca = new TextBox { Location = new Point(140, 86), Size = new Size(150, 20), ReadOnly = true, BackColor = Color.White };

            var lblMarca = new Label { Text = "Marca:", AutoSize = true, Location = new Point(20, 120) };
            txtMarca = new TextBox { Location = new Point(140, 116), Size = new Size(180, 20), ReadOnly = true, BackColor = Color.White };

            var lblModelo = new Label { Text = "Modelo:", AutoSize = true, Location = new Point(20, 150) };
            txtModelo = new TextBox { Location = new Point(140, 146), Size = new Size(180, 20), ReadOnly = true, BackColor = Color.White };

            var lblCor = new Label { Text = "Cor:", AutoSize = true, Location = new Point(20, 180) };
            txtCor = new TextBox { Location = new Point(140, 176), Size = new Size(150, 20), ReadOnly = true, BackColor = Color.White };

            var lblObs = new Label { Text = "Observações:", AutoSize = true, Location = new Point(20, 210) };
            txtObservacoes = new TextBox
            {
                Location = new Point(140, 210),
                Size = new Size(340, 90),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.White,
                ScrollBars = ScrollBars.Vertical
            };

            statusBar = new StatusStrip();
            lblStatus = new ToolStripStatusLabel("Selecione um veículo acima.");
            statusBar.Items.Add(lblStatus);
            statusBar.Location = new Point(0, 378);

            Controls.AddRange(new Control[] {
                lblSel, cmbVeiculo, lblCliente, txtCliente, lblPlaca, txtPlaca,
                lblMarca, txtMarca, lblModelo, txtModelo, lblCor, txtCor,
                lblObs, txtObservacoes, statusBar
            });
        }

        private void CarregarVeiculos()
        {
            cmbVeiculo.Items.Clear();
            cmbVeiculo.SelectedIndex = -1;
            foreach (Veiculo v in VeiculoDAO.Listar(null))
            {
                cmbVeiculo.Items.Add(v);
            }
            if (cmbVeiculo.Items.Count > 0) cmbVeiculo.SelectedIndex = 0;
        }

        private void ExibirDetalhes()
        {
            var v = cmbVeiculo.SelectedItem as Veiculo;
            if (v == null) return;

            txtPlaca.Text = v.Placa;
            txtMarca.Text = v.Marca;
            txtModelo.Text = v.Modelo;
            txtCor.Text = v.Cor;
            txtObservacoes.Text = v.Observacoes;
            txtCliente.Text = v.NomeCliente;

            lblStatus.Text = cmbVeiculo.Items.Count + " veículo(s) cadastrado(s).";
        }
    }
}