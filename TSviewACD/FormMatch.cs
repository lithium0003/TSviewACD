using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSviewACD
{
    public partial class FormMatch : Form
    {
        public FormMatch()
        {
            InitializeComponent();
        }

        private CancellationTokenSource ct_source = new CancellationTokenSource();

        public ListViewItem[] SelectedRemoteFiles;
        
        private void button_AddFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
            
            listBox1.Items.AddRange(openFileDialog1.FileNames.Where(x => listBox1.Items.IndexOf(x) < 0).Select(x => "  "+x).ToArray());
        }

        private void DoDirectoryAdd(IEnumerable<string> Filenames)
        {
            foreach (var filename in Filenames)
            {
                listBox1.Items.AddRange(Directory.EnumerateFiles(filename).Where(x => listBox1.Items.IndexOf(x) < 0).Select(x => "  " + x).ToArray());

                DoDirectoryAdd(Directory.EnumerateDirectories(filename));
            }
        }

        private void button_AddFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK) return;

            label_info.Text = "Add Folder ...";
            try
            {
                DoDirectoryAdd(new string[] { folderBrowserDialog1.SelectedPath });
            }
            catch { }
            label_info.Text = "";
        }

        private void deltetItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach(var i in listBox1.SelectedIndices.OfType<int>().Reverse())
            {
                listBox1.Items.RemoveAt(i);
            }
        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == (Keys.A | Keys.Control))
            {
                for (var i = 0; i < listBox1.Items.Count; i++)
                    listBox1.SetSelected(i, true);
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                button_start.Enabled = false;
                for (var i = 0; i < listBox1.Items.Count; i++)
                {
                    var filename = listBox1.Items[i] as string;
                    var name = Path.GetFileName(filename);

                    var matchitem = SelectedRemoteFiles?.Select(x => (x.Tag as ItemInfo).info).Where(x => x.kind != "FOLDER" && x.name == name);
                    if (matchitem.Count() > 0)
                    {
                        bool match = false;
                        foreach (var item in matchitem)
                        {
                            if (new System.IO.FileInfo(filename.Substring(2)).Length == item.contentProperties?.size)
                            {
                                match = true;
                                break;
                            }
                        }
                        if (match)
                        {
                            //match
                            listBox1.Items[i] = "R " + filename.Substring(2);
                        }
                        else
                        {
                            //no match
                            listBox1.Items[i] = "C " + filename.Substring(2);
                        }


                        if (match && checkBox_MD5.Checked)
                        {
                            byte[] md5 = null;
                            label_info.Text = "Check file MD5...";
                            using (var md5calc = new System.Security.Cryptography.MD5CryptoServiceProvider())
                            using (var hfile = File.Open(filename.Substring(2), FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                await Task.Run(() => { md5 = md5calc.ComputeHash(hfile); }, ct_source.Token);
                                label_info.Text = "";
                                match = false;
                                foreach (var item in matchitem)
                                {
                                    if (BitConverter.ToString(md5).ToLower().Replace("-", "") == item.contentProperties?.md5)
                                    {
                                        //match
                                        match = true;
                                        break;
                                    }

                                }
                                //MD5 not match
                                listBox1.Items[i] = ((match) ? "Ro" : "Rx") + filename.Substring(2);
                            }
                        }
                    }
                    else
                    {
                        //nomatch
                        listBox1.Items[i] = "L " + filename.Substring(2);
                    }
                }
                button_start.Enabled = true;
            }
            catch (OperationCanceledException)
            {

            }
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            //背景を描画する
            //項目が選択されている時は強調表示される

            //ListBoxが空のときにListBoxが選択されるとe.Indexが-1になる
            if (e.Index > -1)
            { //空でない場合
                string txt = ((ListBox)sender).Items[e.Index].ToString();
                //描画する文字列の取得

                if ((e.State & DrawItemState.Selected) != DrawItemState.Selected)
                { //選択されていない時
                    if (txt.StartsWith("L"))
                    {
                        // ローカルのみ
                        e.Graphics.FillRectangle(Brushes.LemonChiffon, e.Bounds);
                    }
                    else if (txt.StartsWith("C"))
                    {
                        // リモートに同名ファイルがあるが一致していない
                        e.Graphics.FillRectangle(Brushes.LightPink, e.Bounds);
                    }
                    else if (txt.StartsWith("Ro"))
                    {
                        // リモートと一致 MD5
                        e.Graphics.FillRectangle(Brushes.MediumSeaGreen, e.Bounds);
                    }
                    else if (txt.StartsWith("Rx"))
                    {
                        // リモートと不一致 MD5
                        e.Graphics.FillRectangle(Brushes.DeepPink, e.Bounds);
                    }
                    else if (txt.StartsWith("R"))
                    {
                        // リモートと一致
                        e.Graphics.FillRectangle(Brushes.LightGreen, e.Bounds);
                    }
                }

                using (Brush b = new SolidBrush(e.ForeColor))
                    e.Graphics.DrawString(txt, e.Font, b, e.Bounds);
            }

            e.DrawFocusRectangle();
            //フォーカスを示す四角形を描画
        }

        private void FormMatch_FormClosing(object sender, FormClosingEventArgs e)
        {
            ct_source.Cancel();
        }
    }
}
