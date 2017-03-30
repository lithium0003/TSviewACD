using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSviewACD
{
    public partial class FormFFmoduleConfig : Form
    {
        int oldDpi;
        int currentDpi;

        public FormFFmoduleConfig()
        {
            InitializeComponent();
            float dx, dy;
            using (var g = CreateGraphics())
            {
                dx = g.DpiX;
                dy = g.DpiY;

            }
            currentDpi = (int)dx;
            HandleDpiChanged();
        }


        const int WM_DPICHANGED = 0x02E0;

        private bool needAdjust = false;
        private bool isMoving = false;

        protected override void OnResizeBegin(EventArgs e)
        {
            base.OnResizeBegin(e);
            isMoving = true;
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            isMoving = false;
            if (needAdjust)
            {
                needAdjust = false;
                HandleDpiChanged();
            }
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            if (needAdjust && IsLocationGood())
            {
                needAdjust = false;
                HandleDpiChanged();
            }
        }

        private bool IsLocationGood()
        {
            if (oldDpi == 0) return false;

            float scaleFactor = (float)currentDpi / oldDpi;
            Config.Log.LogOut(string.Format("c:{0} o:{1} scale{2}", currentDpi, oldDpi, scaleFactor));

            int widthDiff = (int)(Bounds.Width * scaleFactor) - Bounds.Width;
            int heightDiff = (int)(Bounds.Height * scaleFactor) - Bounds.Height;

            var rect = new W32.RECT()
            {
                left = Bounds.Left,
                top = Bounds.Top,
                right = Bounds.Right + widthDiff,
                bottom = Bounds.Bottom + heightDiff
            };

            var handleMonitor = W32.MonitorFromRect(ref rect, W32.MONITOR_DEFAULTTONULL);

            if (handleMonitor != IntPtr.Zero)
            {
                if (W32.GetDpiForMonitor(handleMonitor, W32.Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint dpiY) == 0)
                {
                    if (dpiX == currentDpi)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_DPICHANGED:
                    oldDpi = currentDpi;
                    currentDpi = m.WParam.ToInt32() & 0xFFFF;

                    if (oldDpi != currentDpi)
                    {
                        if (isMoving)
                        {
                            needAdjust = true;
                        }
                        else
                        {
                            HandleDpiChanged();
                        }
                    }
                    break;
            }

            base.WndProc(ref m);
        }


        const int designTimeDpi = 96;

        private void HandleDpiChanged()
        {
            if (oldDpi != 0)
            {
                float scaleFactor = (float)currentDpi / oldDpi;
                Config.Log.LogOut(string.Format("Dpi c:{0} o:{1} scale{2}", currentDpi, oldDpi, scaleFactor));

                //the default scaling method of the framework
                Scale(new SizeF(scaleFactor, scaleFactor));

                //fonts are not scaled automatically so we need to handle this manually
                ScaleFonts(scaleFactor);

                //perform any other scaling different than font or size (e.g. ItemHeight)
                PerformSpecialScaling(scaleFactor);
            }
            else
            {
                //the special scaling also needs to be done initially
                PerformSpecialScaling((float)currentDpi / designTimeDpi);
            }
        }

        protected virtual void PerformSpecialScaling(float scaleFactor)
        {
            foreach(ColumnHeader c in listView1.Columns)
            {
                c.Width = (int)(c.Width * scaleFactor);
            }
        }

        protected virtual void ScaleFonts(float scaleFactor)
        {
            Font = new Font(Font.FontFamily,
                   Font.Size * scaleFactor,
                   Font.Style);
            //ScaleFontForControl(this, scaleFactor);
        }

        private static void ScaleFontForControl(Control control, float factor)
        {
            control.Font = new Font(control.Font.FontFamily,
                   control.Font.Size * factor,
                   control.Font.Style);

            foreach (Control child in control.Controls)
            {
                ScaleFontForControl(child, factor);
            }
        }


        private void LoadData()
        {
            textBox_fontpath.Text = Config.FontFilepath;
            numericUpDown_FontSize.Value = Config.FontPtSize;
            textBox_timeout.Text = Config.FFmodule_TransferLimit.ToString();
            checkBox_autosize.Checked = Config.FFmodule_AutoResize;

            foreach(var item in Config.FFmoduleKeybinds)
            {
                foreach (var akey in item.Value)
                {
                    var listitem = new ListViewItem(new string[] { "", "" });
                    listitem.SubItems[1].Text = akey.ToString();
                    switch (item.Key)
                    {
                        case ffmodule.FFplayerKeymapFunction.FuncPlayExit:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncPlayExit;
                            listitem.Text = Resource_text.FuncPlayExit_str;
                            listitem.ToolTipText = Resource_text.FuncPlayExit_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncSeekMinus10sec:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncSeekMinus10sec;
                            listitem.Text = Resource_text.FuncSeekMinus10sec_str;
                            listitem.ToolTipText = Resource_text.FuncSeekMinus10sec_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncSeekMinus60sec:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncSeekMinus60sec;
                            listitem.Text = Resource_text.FuncSeekMinus60sec_str;
                            listitem.ToolTipText = Resource_text.FuncSeekMinus60sec_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncSeekPlus10sec:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncSeekPlus10sec;
                            listitem.Text = Resource_text.FuncSeekPlus10sec_str;
                            listitem.ToolTipText = Resource_text.FuncSeekPlus10sec_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncSeekPlus60sec:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncSeekPlus60sec;
                            listitem.Text = Resource_text.FuncSeekPlus60sec_str;
                            listitem.ToolTipText = Resource_text.FuncSeekPlus60sec_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncToggleFullscreen:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncToggleFullscreen;
                            listitem.Text = Resource_text.FuncToggleFullscreen_str;
                            listitem.ToolTipText = Resource_text.FuncToggleFullscreen_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncVolumeDown:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncVolumeDown;
                            listitem.Text = Resource_text.FuncVolumeDown_str;
                            listitem.ToolTipText = Resource_text.FuncVolumeDown_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncVolumeUp:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncVolumeUp;
                            listitem.Text = Resource_text.FuncVolumeUp_str;
                            listitem.ToolTipText = Resource_text.FuncVolumeUp_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncToggleDisplay:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncToggleDisplay;
                            listitem.Text = Resource_text.FuncToggleDisplay_str;
                            listitem.ToolTipText = Resource_text.FuncToggleDisplay_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncToggleMute:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncToggleMute;
                            listitem.Text = Resource_text.FuncToggleMute_str;
                            listitem.ToolTipText = Resource_text.FuncToggleMute_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncCycleChannel:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncCycleChannel;
                            listitem.Text = Resource_text.FuncCycleChannel_str;
                            listitem.ToolTipText = Resource_text.FuncCycleChannel_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncCycleAudio:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncCycleAudio;
                            listitem.Text = Resource_text.FuncCycleAudio_str;
                            listitem.ToolTipText = Resource_text.FuncCycleAudio_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncCycleSubtitle:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncCycleSubtitle;
                            listitem.Text = Resource_text.FuncCycleSubtitle_str;
                            listitem.ToolTipText = Resource_text.FuncCycleSubtitle_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncForwardChapter:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncForwardChapter;
                            listitem.Text = Resource_text.FuncForwardChapter_str;
                            listitem.ToolTipText = Resource_text.FuncForwardChapter_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncRewindChapter:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncRewindChapter;
                            listitem.Text = Resource_text.FuncRewindChapter_str;
                            listitem.ToolTipText = Resource_text.FuncRewindChapter_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncTogglePause:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncTogglePause;
                            listitem.Text = Resource_text.FuncTogglePause_str;
                            listitem.ToolTipText = Resource_text.FuncTogglePause_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncResizeOriginal:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncResizeOriginal;
                            listitem.Text = Resource_text.FuncResizeOriginal_str;
                            listitem.ToolTipText = Resource_text.FuncResizeOriginal_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncSrcVolumeUp:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncSrcVolumeUp;
                            listitem.Text = Resource_text.FuncSrcVolumeUp_str;
                            listitem.ToolTipText = Resource_text.FuncSrcVolumeUp_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncSrcVolumeDown:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncSrcVolumeDown;
                            listitem.Text = Resource_text.FuncSrcVolumeDown_str;
                            listitem.ToolTipText = Resource_text.FuncSrcVolumeDown_tip_str;
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncSrcAutoVolume:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncSrcAutoVolume;
                            listitem.Text = Resource_text.FuncSrcAutoVolume_str;
                            listitem.ToolTipText = Resource_text.FuncSrcAutoVolume_tip_str;
                            break;
                        default:
                            continue;
                    }
                    listView1.Items.Add(listitem);
                }
            }
            listView1.Sorting = SortOrder.Ascending;
            listView1.Sort();
        }

        private void SaveData()
        {
            Config.FFmoduleKeybinds.Clear();
            var keyconverter = new KeysConverter();
            foreach (ListViewItem item in listView1.Items)
            {
                var command = (ffmodule.FFplayerKeymapFunction)(item.Tag);
                FFmoduleKeysClass key_array;
                Config.FFmoduleKeybinds.TryGetValue(command, out key_array);
                key_array = key_array ?? new FFmoduleKeysClass();
                key_array.Add((Keys)(keyconverter.ConvertFromString(item.SubItems[1].Text)));
                Config.FFmoduleKeybinds[command] = key_array;
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(listView1.SelectedItems.Count > 0)
            {
                var item = listView1.SelectedItems[0];
                textBox_keyenter.Text = item.SubItems[1].Text;
            }
        }

        private void textBox_keyenter_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            e.Handled = true;
            textBox_keyenter.Text = e.KeyCode.ToString();
            if (listView1.SelectedItems.Count > 0)
            {
                var item = listView1.SelectedItems[0];
                item.SubItems[1].Text = textBox_keyenter.Text;
                SaveData();
            }
        }

        private void FormFFmoduleConfig_Load(object sender, EventArgs e)
        {
            LoadData();
        }

        private void listView1_Click(object sender, EventArgs e)
        {
            textBox_keyenter.Focus();
        }

        private void FormFFmoduleConfig_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveData();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = textBox_fontpath.Text;
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox_fontpath.Text = openFileDialog1.FileName;
            }
        }

        private void textBox_fontpath_TextChanged(object sender, EventArgs e)
        {
            if (File.Exists(textBox_fontpath.Text))
            {
                Config.FontFilepath = textBox_fontpath.Text;
            }
        }

        private void numericUpDown_FontSize_ValueChanged(object sender, EventArgs e)
        {
            Config.FontPtSize = (int)numericUpDown_FontSize.Value;
        }

        private void addAnotherKeyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                var item = listView1.SelectedItems[0];
                listView1.Items.Add(item.Clone() as ListViewItem);
                listView1.Sort();
            }
            SaveData();
        }

        private void deleteKeyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                var item = listView1.SelectedItems[0];
                var other = listView1.Items.Cast<ListViewItem>().Where(x => x.Text == item.Text).ToArray();
                if(other.Count() > 1)
                {
                    listView1.Items.Remove(item);
                }
                else
                {
                    item.SubItems[1].Text = Keys.None.ToString();
                }
            }
            SaveData();
        }

        private void textBox_timeout_TextChanged(object sender, EventArgs e)
        {
            double.TryParse(textBox_timeout.Text, out Config.FFmodule_TransferLimit);
            textBox_timeout.Text = Config.FFmodule_TransferLimit.ToString();
        }

        private void checkBox_autosize_CheckedChanged(object sender, EventArgs e)
        {
            Config.FFmodule_AutoResize = checkBox_autosize.Checked;
        }
    }
}
