using System;
using System.Drawing;
using System.Windows.Forms;

namespace Soen___Torrezim
{
    /// <summary>Sobre o tema/crédito do sistema.</summary>
    public partial class DeepDarkness : Form
    {
        private int _cor = 0;

        public DeepDarkness()
        {
            InitializeComponent();
            Text = "Soen - Tema";
            ClientSize = new Size(400, 250);
            StartPosition = FormStartPosition.CenterParent;

            BackColor = Color.FromArgb(24, 24, 24);
            ForeColor = Color.LightGray;

            var l1 = new Label
            {
                Text = "Tema 'Deep Darkness'" + Environment.NewLine +
                       "Interfaces escuras com foco no conforto visual.",
                Location = new Point(12, 20),
                AutoSize = true,
                ForeColor = Color.LightSlateGray
            };
            var btnTrocarFundo = UIHelpers.CreateButton("Alternar Fundo", new Point(12, 120), new Size(140, 28));
            btnTrocarFundo.Click += (s, e) => { _cor++; btnTrocarFundo.BackColor = _cor % 2 == 0 ? Color.FromArgb(24, 24, 24) : Color.FromArgb(40, 40, 40); };
            var btnOk = UIHelpers.CreateButton("Fechar", new Point(170, 120), new Size(90, 28));
            btnOk.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { l1, btnTrocarFundo, btnOk });
        }
    }
}