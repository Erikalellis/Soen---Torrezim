using System;
using System.Drawing;
using System.Windows.Forms;

namespace Soen___Torrezim
{
    /// <summary>Verificação de Atualizações (informativo).</summary>
    public partial class Atualizar : BaseForm
    {
        public Atualizar()
        {
            InitializeComponent();
            Text = "Soen - Atualizações";
            ClientSize = new Size(420, 220);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = SystemColors.GradientInactiveCaption;

            var l1 = new Label
            {
                Text = "SOEN - Sistema de Oficina",
                Location = new Point(12, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            var l2 = new Label
            {
                Text = "Versão atual: " + Application.ProductVersion + Environment.NewLine +
                       "Seu sistema está atualizado." + Environment.NewLine +
                       "Novidades e correções serão distribuídas por atualizações futuras.",
                Location = new Point(12, 60),
                AutoSize = true
            };
            var btnOk = UIHelpers.CreateButton("OK", new Point(160, 150), new Size(90, 28));
            btnOk.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { l1, l2, btnOk });
        }
    }
}