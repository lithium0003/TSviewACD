using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSviewACD
{
    public partial class FormInputName : Form
    {
        public FormInputName()
        {
            InitializeComponent();
        }

        public string NewItemName { get { return textBox1.Text; } set { textBox1.Text = value; } }
    }
}
