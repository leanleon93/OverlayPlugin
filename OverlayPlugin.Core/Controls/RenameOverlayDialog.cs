using System;
using System.Windows.Forms;

namespace RainbowMage.OverlayPlugin.Controls
{
    public partial class RenameOverlayDialog : Form
    {
        public string OverlayName { get => txtName.Text; }

        public RenameOverlayDialog(string name)
        {
            InitializeComponent();

            txtName.Text = name;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }
    }
}
