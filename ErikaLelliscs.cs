using System;
using System.Drawing;
using System.Windows.Forms;

namespace Soen___Torrezim
{
    /// <summary>Créditos do sistema.</summary>
    public partial class ErikaLelliscs : Form
    {
        public ErikaLelliscs()
        {
            InitializeComponent();
            Text = "Soen - Sobre";
            ClientSize = new Size(420, 260);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;

            var l1 = new Label
            {
                Text = "SOEN - Sistema de Oficina" + Environment.NewLine +
                       "Versão 1.0" + Environment.NewLine +
                       "Desenvolvido por Erika Lellis" + Environment.NewLine + Environment.NewLine +
                       "Gerenciamento de oficina: clientes, veículos," + Environment.NewLine +
                       "serviços, agendamentos, orçamentos, vendas," + Environment.NewLine +
                       "estoque e financeiro.",
                Location = new Point(12, 20),
                AutoSize = true
            };
            var btnOk = new Button { Text = "OK", Location = new Point(160, 190), Size = new Size(90, 28) };
            btnOk.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { l1, btnOk });
        }
    }
}