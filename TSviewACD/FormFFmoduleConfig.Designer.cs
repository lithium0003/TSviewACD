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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormFFmoduleConfig));
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox_Mouse = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_keyenter = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.checkBox_autosize = new System.Windows.Forms.CheckBox();
            this.groupBox_keybord = new System.Windows.Forms.GroupBox();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader_Command = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_KeyBind = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deleteKeyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addAnotherKeyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox_font = new System.Windows.Forms.GroupBox();
            this.numericUpDown_FontSize = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.textBox_fontpath = new System.Windows.Forms.TextBox();
            this.groupBox_speed = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox_timeout = new System.Windows.Forms.TextBox();
            this.groupBox_Screen = new System.Windows.Forms.GroupBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.groupBox_Mouse.SuspendLayout();
            this.groupBox_keybord.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.groupBox_font.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_FontSize)).BeginInit();
            this.groupBox_speed.SuspendLayout();
            this.groupBox_Screen.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            resources.ApplyResources(this.button1, "button1");
            this.button1.Name = "button1";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // groupBox_Mouse
            // 
            this.groupBox_Mouse.Controls.Add(this.label2);
            this.groupBox_Mouse.Controls.Add(this.label1);
            resources.ApplyResources(this.groupBox_Mouse, "groupBox_Mouse");
            this.groupBox_Mouse.Name = "groupBox_Mouse";
            this.groupBox_Mouse.TabStop = false;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            this.toolTip1.SetToolTip(this.label3, resources.GetString("label3.ToolTip"));
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            this.toolTip1.SetToolTip(this.label4, resources.GetString("label4.ToolTip"));
            // 
            // textBox_keyenter
            // 
            this.textBox_keyenter.AcceptsReturn = true;
            this.textBox_keyenter.AcceptsTab = true;
            resources.ApplyResources(this.textBox_keyenter, "textBox_keyenter");
            this.textBox_keyenter.Name = "textBox_keyenter";
            this.toolTip1.SetToolTip(this.textBox_keyenter, resources.GetString("textBox_keyenter.ToolTip"));
            this.textBox_keyenter.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox_keyenter_KeyDown);
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // checkBox_autosize
            // 
            resources.ApplyResources(this.checkBox_autosize, "checkBox_autosize");
            this.checkBox_autosize.Name = "checkBox_autosize";
            this.checkBox_autosize.UseVisualStyleBackColor = true;
            this.checkBox_autosize.CheckedChanged += new System.EventHandler(this.checkBox_autosize_CheckedChanged);
            // 
            // groupBox_keybord
            // 
            this.groupBox_keybord.Controls.Add(this.label3);
            this.groupBox_keybord.Controls.Add(this.listView1);
            this.groupBox_keybord.Controls.Add(this.textBox_keyenter);
            resources.ApplyResources(this.groupBox_keybord, "groupBox_keybord");
            this.groupBox_keybord.Name = "groupBox_keybord";
            this.groupBox_keybord.TabStop = false;
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
            resources.ApplyResources(this.listView1, "listView1");
            this.listView1.Name = "listView1";
            this.listView1.ShowItemToolTips = true;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.SelectedIndexChanged += new System.EventHandler(this.listView1_SelectedIndexChanged);
            this.listView1.Click += new System.EventHandler(this.listView1_Click);
            // 
            // columnHeader_Command
            // 
            resources.ApplyResources(this.columnHeader_Command, "columnHeader_Command");
            // 
            // columnHeader_KeyBind
            // 
            resources.ApplyResources(this.columnHeader_KeyBind, "columnHeader_KeyBind");
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteKeyToolStripMenuItem,
            this.addAnotherKeyToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            resources.ApplyResources(this.contextMenuStrip1, "contextMenuStrip1");
            // 
            // deleteKeyToolStripMenuItem
            // 
            this.deleteKeyToolStripMenuItem.Name = "deleteKeyToolStripMenuItem";
            resources.ApplyResources(this.deleteKeyToolStripMenuItem, "deleteKeyToolStripMenuItem");
            this.deleteKeyToolStripMenuItem.Click += new System.EventHandler(this.deleteKeyToolStripMenuItem_Click);
            // 
            // addAnotherKeyToolStripMenuItem
            // 
            this.addAnotherKeyToolStripMenuItem.Name = "addAnotherKeyToolStripMenuItem";
            resources.ApplyResources(this.addAnotherKeyToolStripMenuItem, "addAnotherKeyToolStripMenuItem");
            this.addAnotherKeyToolStripMenuItem.Click += new System.EventHandler(this.addAnotherKeyToolStripMenuItem_Click);
            // 
            // groupBox_font
            // 
            this.groupBox_font.Controls.Add(this.numericUpDown_FontSize);
            this.groupBox_font.Controls.Add(this.label5);
            this.groupBox_font.Controls.Add(this.button2);
            this.groupBox_font.Controls.Add(this.label4);
            this.groupBox_font.Controls.Add(this.textBox_fontpath);
            resources.ApplyResources(this.groupBox_font, "groupBox_font");
            this.groupBox_font.Name = "groupBox_font";
            this.groupBox_font.TabStop = false;
            // 
            // numericUpDown_FontSize
            // 
            resources.ApplyResources(this.numericUpDown_FontSize, "numericUpDown_FontSize");
            this.numericUpDown_FontSize.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDown_FontSize.Name = "numericUpDown_FontSize";
            this.numericUpDown_FontSize.Value = new decimal(new int[] {
            12,
            0,
            0,
            0});
            this.numericUpDown_FontSize.ValueChanged += new System.EventHandler(this.numericUpDown_FontSize_ValueChanged);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // button2
            // 
            resources.ApplyResources(this.button2, "button2");
            this.button2.Name = "button2";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // textBox_fontpath
            // 
            resources.ApplyResources(this.textBox_fontpath, "textBox_fontpath");
            this.textBox_fontpath.Name = "textBox_fontpath";
            this.textBox_fontpath.TextChanged += new System.EventHandler(this.textBox_fontpath_TextChanged);
            // 
            // groupBox_speed
            // 
            this.groupBox_speed.Controls.Add(this.label7);
            this.groupBox_speed.Controls.Add(this.textBox_timeout);
            this.groupBox_speed.Controls.Add(this.label6);
            resources.ApplyResources(this.groupBox_speed, "groupBox_speed");
            this.groupBox_speed.Name = "groupBox_speed";
            this.groupBox_speed.TabStop = false;
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // textBox_timeout
            // 
            resources.ApplyResources(this.textBox_timeout, "textBox_timeout");
            this.textBox_timeout.Name = "textBox_timeout";
            this.textBox_timeout.TextChanged += new System.EventHandler(this.textBox_timeout_TextChanged);
            // 
            // groupBox_Screen
            // 
            this.groupBox_Screen.Controls.Add(this.checkBox_autosize);
            resources.ApplyResources(this.groupBox_Screen, "groupBox_Screen");
            this.groupBox_Screen.Name = "groupBox_Screen";
            this.groupBox_Screen.TabStop = false;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // FormFFmoduleConfig
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.groupBox_Screen);
            this.Controls.Add(this.groupBox_speed);
            this.Controls.Add(this.groupBox_font);
            this.Controls.Add(this.groupBox_keybord);
            this.Controls.Add(this.groupBox_Mouse);
            this.Controls.Add(this.button1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "FormFFmoduleConfig";
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
            this.groupBox_Screen.ResumeLayout(false);
            this.groupBox_Screen.PerformLayout();
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
        private System.Windows.Forms.GroupBox groupBox_Screen;
        private System.Windows.Forms.CheckBox checkBox_autosize;
    }
}