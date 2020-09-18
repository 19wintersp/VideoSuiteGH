using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.FFMPEG;

namespace Keyframer
{
    public partial class KeyframerApp : Form
    {

        public Button button1;
        public CheckBox cbox1;
        private ToolStripContainer toolStripContainer1;
        private ToolStrip toolStrip1;

        public KeyframerApp()
        {
            button1 = new Button();
            button1.Size = new Size(160, 25);
            button1.Location = new Point(30, 30);
            button1.Text = "Enable da Checkbox";
            this.Controls.Add(button1);
            button1.Click += new EventHandler(b1click);
            cbox1 = new CheckBox();
            cbox1.Size = new Size(20, 20);
            cbox1.Location = new Point(30, 80);
            cbox1.Enabled = false;
            this.Controls.Add(cbox1);
            cbox1.Click += new EventHandler(c1click);
            toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            // Add items to the ToolStrip.
            toolStrip1.Items.Add("One");
            toolStrip1.Items.Add("Two");
            toolStrip1.Items.Add("Three");
            // Add the ToolStrip to the top panel of the ToolStripContainer.
            toolStripContainer1.TopToolStripPanel.Controls.Add(toolStrip1);
            // Add the ToolStripContainer to the form.
            Controls.Add(toolStripContainer1);
            InitializeComponent();
        }

        private void b1click(object sender, EventArgs e)
        {
            cbox1.Enabled = true;
            DialogResult dresult = MessageBox.Show("Checkbox enabled.", "You clicked the button.", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (dresult == DialogResult.Cancel)
            {
                cbox1.Enabled = false;
                MessageBox.Show("Disabled checkbox.", "Undone", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        private void c1click(object sender, EventArgs e)
        {
            if (((CheckBox) sender).Checked)
            {
                MessageBox.Show("Checked!");
            } else
            {
                Application.Exit();
                return;
            }
        }
    }
}
