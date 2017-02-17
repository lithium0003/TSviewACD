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
        public FormFFmoduleConfig()
        {
            InitializeComponent();
        }

        private void LoadData()
        {
            textBox_fontpath.Text = Config.FontFilepath;
            numericUpDown_FontSize.Value = Config.FontPtSize;
            textBox_timeout.Text = Config.FFmodule_TransferLimit.ToString();

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
                            listitem.Text = "Exit";
                            listitem.ToolTipText = "再生を終了しウインドウを閉じます";
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncSeekMinus10sec:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncSeekMinus10sec;
                            listitem.Text = "Rewind 10sec";
                            listitem.ToolTipText = "10秒 戻します";
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncSeekMinus60sec:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncSeekMinus60sec;
                            listitem.Text = "Rewind 60sec";
                            listitem.ToolTipText = "60秒 戻します";
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncSeekPlus10sec:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncSeekPlus10sec;
                            listitem.Text = "Forward 10sec";
                            listitem.ToolTipText = "10秒 進めます";
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncSeekPlus60sec:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncSeekPlus60sec;
                            listitem.Text = "Forward 60sec";
                            listitem.ToolTipText = "60秒 進めます";
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncToggleFullscreen:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncToggleFullscreen;
                            listitem.Text = "Toggle Fullscreen";
                            listitem.ToolTipText = "全画面表示とウインドウ表示を切り替えます";
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncVolumeDown:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncVolumeDown;
                            listitem.Text = "Volude Down";
                            listitem.ToolTipText = "音量を下げます";
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncVolumeUp:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncVolumeUp;
                            listitem.Text = "Volude Up";
                            listitem.ToolTipText = "音量を上げます";
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncToggleDisplay:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncToggleDisplay;
                            listitem.Text = "Toggle Display";
                            listitem.ToolTipText = "画面に時間表示を出します";
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncToggleMute:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncToggleMute;
                            listitem.Text = "Mute";
                            listitem.ToolTipText = "音声のミュートを切り替えます";
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncCycleChannel:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncCycleChannel;
                            listitem.Text = "Cycle Channel";
                            listitem.ToolTipText = "動画および関連づけられた音声と字幕を切り替えます";
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncCycleAudio:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncCycleAudio;
                            listitem.Text = "Cycle Audio";
                            listitem.ToolTipText = "音声を切り替えます";
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncCycleSubtitle:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncCycleSubtitle;
                            listitem.Text = "Cycle Subtitle";
                            listitem.ToolTipText = "字幕を切り替えます";
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncForwardChapter:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncForwardChapter;
                            listitem.Text = "Seek Forward Chapter";
                            listitem.ToolTipText = "チャプターを進めます";
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncRewindChapter:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncRewindChapter;
                            listitem.Text = "Seek Rewind Chapter";
                            listitem.ToolTipText = "チャプターを戻します";
                            break;
                        case ffmodule.FFplayerKeymapFunction.FuncTogglePause:
                            listitem.Tag = ffmodule.FFplayerKeymapFunction.FuncTogglePause;
                            listitem.Text = "Toggle Pause";
                            listitem.ToolTipText = "一時停止を切り替えます";
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
    }
}
