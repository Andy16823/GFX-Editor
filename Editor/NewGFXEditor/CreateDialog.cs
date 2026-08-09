using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NewGFXEditor.Viewmodel
{
    public partial class CreateDialog : Form
    {
        public string Value { get; set; }

        public CreateDialog(String title = "Create new Object", string initialValue = "")
        {
            InitializeComponent();
            this.Text = title;
            this.Value = initialValue;
            this.textBox1.Text = initialValue;
        }

        private void CreateDialog_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Value = textBox1.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
