using System;
using System.Drawing;
using System.Windows.Forms;

namespace Soen___Torrezim
{
    public class BaseForm : Form
    {
        protected StatusStrip BaseStatusStrip;
        protected ToolStripStatusLabel BaseStatusLabel;

        public BaseForm()
        {
            // Estilo padrão
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            BackColor = SystemColors.GradientInactiveCaption;
            StartPosition = FormStartPosition.CenterParent;

            // StatusStrip padrão
            BaseStatusStrip = new StatusStrip();
            BaseStatusLabel = new ToolStripStatusLabel(" ");
            BaseStatusStrip.Items.Add(BaseStatusLabel);
            BaseStatusStrip.Dock = DockStyle.Bottom;
            Controls.Add(BaseStatusStrip);
        }

        protected void SetStatus(string text)
        {
            if (BaseStatusLabel != null) BaseStatusLabel.Text = text;
        }

        protected override void OnLoad(System.EventArgs e)
        {
            base.OnLoad(e);
            StandardizeControls(this);
            ApplyBackgroundFromConfig();
        }

        private void ApplyBackgroundFromConfig()
        {
            try
            {
                var cfg = Data.EmpresaDAO.Obter();
                if (!string.IsNullOrWhiteSpace(cfg.BackgroundImagePath))
                {
                    string path = cfg.BackgroundImagePath;
                    if (System.IO.File.Exists(path))
                    {
                        var img = Image.FromFile(path);
                        this.BackgroundImage = img;
                        switch ((cfg.BackgroundMode ?? "stretch").ToLower())
                        {
                            case "center": this.BackgroundImageLayout = ImageLayout.Center; break;
                            case "tile": this.BackgroundImageLayout = ImageLayout.Tile; break;
                            default: this.BackgroundImageLayout = ImageLayout.Stretch; break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // não interrompe a UI
                Logger.LogError(ex);
            }
        }

        private void StandardizeControls(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Button b)
                {
                    b.Font = new Font("Segoe UI", 9F);
                    b.BackColor = SystemColors.AppWorkspace;
                }
                else if (c is TextBox t)
                {
                    t.Font = new Font("Segoe UI", 9F);
                    t.BackColor = SystemColors.Window;
                }
                else if (c is Label l)
                {
                    l.Font = new Font("Segoe UI", 9F);
                }

                if (c.HasChildren) StandardizeControls(c);
            }
        }
    }
}
