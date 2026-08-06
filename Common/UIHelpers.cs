using System.Drawing;
using System.Windows.Forms;

namespace Soen___Torrezim
{
    public static class UIHelpers
    {
        public static Label CreateLabel(string text, Point location)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Location = location,
                Font = new Font("Segoe UI", 9F)
            };
        }

        public static Button CreateButton(string text, Point location, Size size)
        {
            return new Button
            {
                Text = text,
                Location = location,
                Size = size,
                BackColor = SystemColors.AppWorkspace,
                Font = new Font("Segoe UI", 9F)
            };
        }

        public static TextBox CreateTextBox(Point location, Size size, string text = "")
        {
            return new TextBox
            {
                Location = location,
                Size = size,
                Text = text,
                Font = new Font("Segoe UI", 9F)
            };
        }
    }
}
