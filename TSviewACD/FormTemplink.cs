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
    public partial class FormTemplink : Form
    {
        public FormTemplink()
        {
            InitializeComponent();
        }

        public IEnumerable<FileMetadata_Info> TempLinks
        {
            set
            {
                listView1.Items.Clear();
                listView1.Items.AddRange(value.Select(x => new ListViewItem(new string[] { DriveData.GetFullPathfromId(x.id), x.tempLink })).ToArray());
                listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0) return;
            Clipboard.SetText(string.Join("\n", listView1.SelectedItems.Cast<ListViewItem>().Select(x => x.SubItems[1].Text)));
        }
    }
}
