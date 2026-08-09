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
    /// Descrição detalhada de cada veículo: lista todos os veículos com seus
    /// dados completos (placa, marca, modelo, cor, observações, KM, próxima
    /// revisão e dono), com alerta de revisão vencida/próxima.
    /// </summary>
    public partial class DescricaoVeiculo : BaseForm
    {
        private ComboBox cmbVeiculo;
        private TextBox txtPlaca;
        private TextBox txtMarca;
        private TextBox txtModelo;
        private TextBox txtCor;
        private TextBox txtObservacoes;
        private TextBox txtCliente;
        private TextBox txtKm;
        private TextBox txtRevisao;
        private ToolStripStatusLabel lblStatus;

        public DescricaoVeiculo()
        {
            InitializeComponent();
            Text = "Soen - Descrição Detalhada dos Veículos";
            ClientSize = new Size(560, 500);
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
                Size = new Size(340, 60),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.White,
                ScrollBars = ScrollBars.Vertical
            };

            var lblKm = new Label { Text = "Quilometragem:", AutoSize = true, Location = new Point(20, 282) };
            txtKm = new TextBox { Location = new Point(140, 278), Size = new Size(150, 20), ReadOnly = true, BackColor = Color.White };

            var lblRev = new Label { Text = "Próxima revisão:", AutoSize = true, Location = new Point(20, 312) };
            txtRevisao = new TextBox { Location = new Point(140, 308), Size = new Size(150, 20), ReadOnly = true, BackColor = Color.White };

            // usa StatusStrip padrão da BaseForm
            lblStatus = BaseStatusLabel;

            Controls.AddRange(new Control[] {
                lblSel, cmbVeiculo, lblCliente, txtCliente, lblPlaca, txtPlaca,
                lblMarca, txtMarca, lblModelo, txtModelo, lblCor, txtCor,
                lblObs, txtObservacoes, lblKm, txtKm, lblRev, txtRevisao
            });
        }

        private void CarregarVeiculos()
        {
            cmbVeiculo.Items.Clear();
            cmbVeiculo.SelectedIndex = -1;
            foreach (Veiculo v in VeiculoDAO.Listar(null))
            {
                cmbVeiculo.Items.Add(new ComboVeiculo { Veiculo = v });
            }
            if (cmbVeiculo.Items.Count > 0) cmbVeiculo.SelectedIndex = 0;
        }

        private void ExibirDetalhes()
        {
            var cv = cmbVeiculo.SelectedItem as ComboVeiculo;
            if (cv == null) return;
            var v = cv.Veiculo;

            txtPlaca.Text = v.Placa;
            txtMarca.Text = v.Marca;
            txtModelo.Text = v.Modelo;
            txtCor.Text = v.Cor;
            txtObservacoes.Text = v.Observacoes;
            txtCliente.Text = v.NomeCliente;
            var cult = CultureInfo.GetCultureInfo("pt-BR");
            txtKm.Text = v.Quilometragem > 0 ? v.Quilometragem.ToString("0.##", cult) + " km" : "";

            string status = cmbVeiculo.Items.Count + " veículo(s) cadastrado(s).";
            txtRevisao.Text = "";
            DateTime proxima;
            if (!string.IsNullOrWhiteSpace(v.ProximaRevisao) && DateTime.TryParse(v.ProximaRevisao, out proxima))
            {
                txtRevisao.Text = proxima.ToString("dd/MM/yyyy", cult);
                int dias = (int)(proxima.Date - DateTime.Today).TotalDays;
                if (dias < 0)
                    status = "ATENÇÃO: revisão VENCIDA em " + (-dias) + " dia(s) nesta placa!";
                else if (dias <= 15)
                    status = "Atenção: revisão próxima (" + dias + " dia(s)) — agende a manutenção.";
            }

            lblStatus.Text = status;
        }
    }
}