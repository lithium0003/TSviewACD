namespace TSviewACD
{
    partial class FormFFmoduleConfig
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox_Mouse = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox_keybord = new System.Windows.Forms.GroupBox();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader_Command = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_KeyBind = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deleteKeyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addAnotherKeyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.textBox_keyenter = new System.Windows.Forms.TextBox();
            this.groupBox_font = new System.Windows.Forms.GroupBox();
            this.numericUpDown_FontSize = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.textBox_fontpath = new System.Windows.Forms.TextBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.groupBox_speed = new System.Windows.Forms.GroupBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox_timeout = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.groupBox_Mouse.SuspendLayout();
            this.groupBox_keybord.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.groupBox_font.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_FontSize)).BeginInit();
            this.groupBox_speed.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Location = new System.Drawing.Point(310, 400);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // groupBox_Mouse
            // 
            this.groupBox_Mouse.Controls.Add(this.label2);
            this.groupBox_Mouse.Controls.Add(this.label1);
            this.groupBox_Mouse.Location = new System.Drawing.Point(12, 12);
            this.groupBox_Mouse.Name = "groupBox_Mouse";
            this.groupBox_Mouse.Size = new System.Drawing.Size(221, 99);
            this.groupBox_Mouse.TabIndex = 1;
            this.groupBox_Mouse.TabStop = false;
            this.groupBox_Mouse.Text = "Mouse";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(21, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(191, 36);
            this.label2.TabIndex = 1;
            this.label2.Text = "Right Button:\r\n  drag && click->Seek file to the time\r\n  in ratio X position of w" +
    "indow width";
            this.toolTip1.SetToolTip(this.label2, "右クリックおよび右ボタンを押しながらの移動\r\n　ウインドウの幅をファイルの長さと見なし、x座標の割合の\r\n　場所の時間にシークします。");
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(21, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(177, 24);
            this.label1.TabIndex = 0;
            this.label1.Text = "Left Button:\r\n  double click->Toggle Fullscreen";
            this.toolTip1.SetToolTip(this.label1, "マウス左ダブルクリック\r\n　全画面表示を切り替えます");
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(16, 21);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(126, 24);
            this.label3.TabIndex = 2;
            this.label3.Text = "Select command and \r\npress key in textbox ->";
            this.toolTip1.SetToolTip(this.label3, "以下のコマンドを選択し、右のテキストボックスで\r\n任意のキーを押してキーバインドを設定してください");
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(11, 27);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(132, 12);
            this.label4.TabIndex = 1;
            this.label4.Text = "Font file path for display";
            this.toolTip1.SetToolTip(this.label4, "画面表示に用いるフォントのパス");
            // 
            // groupBox_keybord
            // 
            this.groupBox_keybord.Controls.Add(this.label3);
            this.groupBox_keybord.Controls.Add(this.listView1);
            this.groupBox_keybord.Controls.Add(this.textBox_keyenter);
            this.groupBox_keybord.Location = new System.Drawing.Point(12, 117);
            this.groupBox_keybord.Name = "groupBox_keybord";
            this.groupBox_keybord.Size = new System.Drawing.Size(221, 277);
            this.groupBox_keybord.TabIndex = 2;
            this.groupBox_keybord.TabStop = false;
            this.groupBox_keybord.Text = "Keyboard";
            // 
            // listView1
            // 
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader_Command,
            this.columnHeader_KeyBind});
            this.listView1.ContextMenuStrip = this.contextMenuStrip1;
            this.listView1.FullRowSelect = true;
            this.listView1.GridLines = true;
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(9, 51);
            this.listView1.Name = "listView1";
            this.listView1.ShowItemToolTips = true;
            this.listView1.Size = new System.Drawing.Size(206, 226);
            this.listView1.TabIndex = 1;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.SelectedIndexChanged += new System.EventHandler(this.listView1_SelectedIndexChanged);
            this.listView1.Click += new System.EventHandler(this.listView1_Click);
            // 
            // columnHeader_Command
            // 
            this.columnHeader_Command.Text = "Command";
            this.columnHeader_Command.Width = 120;
            // 
            // columnHeader_KeyBind
            // 
            this.columnHeader_KeyBind.Text = "Keybind";
            this.columnHeader_KeyBind.Width = 70;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteKeyToolStripMenuItem,
            this.addAnotherKeyToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(184, 48);
            // 
            // deleteKeyToolStripMenuItem
            // 
            this.deleteKeyToolStripMenuItem.Name = "deleteKeyToolStripMenuItem";
            this.deleteKeyToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.deleteKeyToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.deleteKeyToolStripMenuItem.Text = "Delete key";
            this.deleteKeyToolStripMenuItem.Click += new System.EventHandler(this.deleteKeyToolStripMenuItem_Click);
            // 
            // addAnotherKeyToolStripMenuItem
            // 
            this.addAnotherKeyToolStripMenuItem.Name = "addAnotherKeyToolStripMenuItem";
            this.addAnotherKeyToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Insert;
            this.addAnotherKeyToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
            this.addAnotherKeyToolStripMenuItem.Text = "Add another key";
            this.addAnotherKeyToolStripMenuItem.Click += new System.EventHandler(this.addAnotherKeyToolStripMenuItem_Click);
            // 
            // textBox_keyenter
            // 
            this.textBox_keyenter.AcceptsReturn = true;
            this.textBox_keyenter.AcceptsTab = true;
            this.textBox_keyenter.Location = new System.Drawing.Point(148, 26);
            this.textBox_keyenter.Multiline = true;
            this.textBox_keyenter.Name = "textBox_keyenter";
            this.textBox_keyenter.Size = new System.Drawing.Size(64, 19);
            this.textBox_keyenter.TabIndex = 0;
            this.textBox_keyenter.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox_keyenter_KeyDown);
            // 
            // groupBox_font
            // 
            this.groupBox_font.Controls.Add(this.numericUpDown_FontSize);
            this.groupBox_font.Controls.Add(this.label5);
            this.groupBox_font.Controls.Add(this.button2);
            this.groupBox_font.Controls.Add(this.label4);
            this.groupBox_font.Controls.Add(this.textBox_fontpath);
            this.groupBox_font.Location = new System.Drawing.Point(239, 12);
            this.groupBox_font.Name = "groupBox_font";
            this.groupBox_font.Size = new System.Drawing.Size(154, 150);
            this.groupBox_font.TabIndex = 3;
            this.groupBox_font.TabStop = false;
            this.groupBox_font.Text = "font";
            // 
            // numericUpDown_FontSize
            // 
            this.numericUpDown_FontSize.Location = new System.Drawing.Point(72, 115);
            this.numericUpDown_FontSize.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDown_FontSize.Name = "numericUpDown_FontSize";
            this.numericUpDown_FontSize.Size = new System.Drawing.Size(74, 19);
            this.numericUpDown_FontSize.TabIndex = 4;
            this.numericUpDown_FontSize.Value = new decimal(new int[] {
            12,
            0,
            0,
            0});
            this.numericUpDown_FontSize.ValueChanged += new System.EventHandler(this.numericUpDown_FontSize_ValueChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(11, 105);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(53, 12);
            this.label5.TabIndex = 3;
            this.label5.Text = "Font Size";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(73, 74);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "select";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // textBox_fontpath
            // 
            this.textBox_fontpath.Location = new System.Drawing.Point(13, 49);
            this.textBox_fontpath.Name = "textBox_fontpath";
            this.textBox_fontpath.Size = new System.Drawing.Size(135, 19);
            this.textBox_fontpath.TabIndex = 0;
            this.textBox_fontpath.TextChanged += new System.EventHandler(this.textBox_fontpath_TextChanged);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // groupBox_speed
            // 
            this.groupBox_speed.Controls.Add(this.label7);
            this.groupBox_speed.Controls.Add(this.textBox_timeout);
            this.groupBox_speed.Controls.Add(this.label6);
            this.groupBox_speed.Location = new System.Drawing.Point(239, 168);
            this.groupBox_speed.Name = "groupBox_speed";
            this.groupBox_speed.Size = new System.Drawing.Size(154, 99);
            this.groupBox_speed.TabIndex = 4;
            this.groupBox_speed.TabStop = false;
            this.groupBox_speed.Text = "Speed Control";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 24);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(142, 24);
            this.label6.TabIndex = 0;
            this.label6.Text = "Disconnect and retry when\r\nthe speed is under";
            this.toolTip1.SetToolTip(this.label6, "転送速度が以下の値を下回った場合は切断して再接続する");
            // 
            // textBox_timeout
            // 
            this.textBox_timeout.Location = new System.Drawing.Point(50, 51);
            this.textBox_timeout.Name = "textBox_timeout";
            this.textBox_timeout.Size = new System.Drawing.Size(56, 19);
            this.textBox_timeout.TabIndex = 1;
            this.textBox_timeout.TextChanged += new System.EventHandler(this.textBox_timeout_TextChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(114, 57);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(35, 12);
            this.label7.TabIndex = 2;
            this.label7.Text = "KiB/s";
            // 
            // FormFFmoduleConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(405, 435);
            this.Controls.Add(this.groupBox_speed);
            this.Controls.Add(this.groupBox_font);
            this.Controls.Add(this.groupBox_keybord);
            this.Controls.Add(this.groupBox_Mouse);
            this.Controls.Add(this.button1);
            this.Name = "FormFFmoduleConfig";
            this.Text = "Config";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormFFmoduleConfig_FormClosed);
            this.Load += new System.EventHandler(this.FormFFmoduleConfig_Load);
            this.groupBox_Mouse.ResumeLayout(false);
            this.groupBox_Mouse.PerformLayout();
            this.groupBox_keybord.ResumeLayout(false);
            this.groupBox_keybord.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            this.groupBox_font.ResumeLayout(false);
            this.groupBox_font.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_FontSize)).EndInit();
            this.groupBox_speed.ResumeLayout(false);
            this.groupBox_speed.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.GroupBox groupBox_Mouse;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox_keybord;
        private System.Windows.Forms.TextBox textBox_keyenter;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader columnHeader_Command;
        private System.Windows.Forms.ColumnHeader columnHeader_KeyBind;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem deleteKeyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addAnotherKeyToolStripMenuItem;
        private System.Windows.Forms.GroupBox groupBox_font;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_fontpath;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.NumericUpDown numericUpDown_FontSize;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.GroupBox groupBox_speed;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBox_timeout;
        private System.Windows.Forms.Label label6;
    }
}