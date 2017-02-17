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
    public partial class FormDriveLog : Form
    {
        public FormDriveLog()
        {
            InitializeComponent();
        }

        public IEnumerable<FileMetadata_Info> ChangeLog
        {
            set
            {
                textBox1.Clear();
                List<string> buf = new List<string>();
                foreach (var node in value)
                {
                    buf.Add(string.Format(
                        "id:{0} name:{1} status:{2} parents:{3} Create:{4} Modified:{5} size:{6} MD5:{7}",
                        node.id,
                        node.name,
                        node.status,
                        string.Join(", ", node.parents),
                        node.createdDate,
                        node.modifiedDate,
                        node.contentProperties?.size,
                        node.contentProperties?.md5));
                }
                textBox1.Lines = buf.ToArray();
            }
        }
    }
}
