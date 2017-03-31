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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_keyenter = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.checkBox_autosize = new System.Windows.Forms.CheckBox();
            this.groupBox_keybord = new System.Windows.Forms.GroupBox();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader_Command = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_KeyBind = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deleteKeyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addAnotherKeyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox_font = new System.Windows.Forms.GroupBox();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.textBox_fontpath = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.numericUpDown_FontSize = new System.Windows.Forms.NumericUpDown();
            this.groupBox_speed = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox_timeout = new System.Windows.Forms.TextBox();
            this.groupBox_Screen = new System.Windows.Forms.GroupBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.groupBox_Mouse.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.groupBox_keybord.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.groupBox_font.SuspendLayout();
            this.flowLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_FontSize)).BeginInit();
            this.groupBox_speed.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.groupBox_Screen.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            resources.ApplyResources(this.button1, "button1");
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Name = "button1";
            this.toolTip1.SetToolTip(this.button1, resources.GetString("button1.ToolTip"));
            this.button1.UseVisualStyleBackColor = true;
            // 
            // groupBox_Mouse
            // 
            resources.ApplyResources(this.groupBox_Mouse, "groupBox_Mouse");
            this.groupBox_Mouse.Controls.Add(this.tableLayoutPanel1);
            this.groupBox_Mouse.Name = "groupBox_Mouse";
            this.groupBox_Mouse.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox_Mouse, resources.GetString("groupBox_Mouse.ToolTip"));
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.toolTip1.SetToolTip(this.tableLayoutPanel1, resources.GetString("tableLayoutPanel1.ToolTip"));
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            this.toolTip1.SetToolTip(this.label1, resources.GetString("label1.ToolTip"));
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            this.toolTip1.SetToolTip(this.label2, resources.GetString("label2.ToolTip"));
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
            this.flowLayoutPanel2.SetFlowBreak(this.textBox_keyenter, true);
            this.textBox_keyenter.Name = "textBox_keyenter";
            this.toolTip1.SetToolTip(this.textBox_keyenter, resources.GetString("textBox_keyenter.ToolTip"));
            this.textBox_keyenter.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox_keyenter_KeyDown);
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.tableLayoutPanel2.SetColumnSpan(this.label6, 2);
            this.label6.Name = "label6";
            this.toolTip1.SetToolTip(this.label6, resources.GetString("label6.ToolTip"));
            // 
            // checkBox_autosize
            // 
            resources.ApplyResources(this.checkBox_autosize, "checkBox_autosize");
            this.checkBox_autosize.Name = "checkBox_autosize";
            this.toolTip1.SetToolTip(this.checkBox_autosize, resources.GetString("checkBox_autosize.ToolTip"));
            this.checkBox_autosize.UseVisualStyleBackColor = true;
            this.checkBox_autosize.CheckedChanged += new System.EventHandler(this.checkBox_autosize_CheckedChanged);
            // 
            // groupBox_keybord
            // 
            resources.ApplyResources(this.groupBox_keybord, "groupBox_keybord");
            this.groupBox_keybord.Controls.Add(this.flowLayoutPanel2);
            this.flowLayoutPanel1.SetFlowBreak(this.groupBox_keybord, true);
            this.groupBox_keybord.Name = "groupBox_keybord";
            this.groupBox_keybord.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox_keybord, resources.GetString("groupBox_keybord.ToolTip"));
            // 
            // flowLayoutPanel2
            // 
            resources.ApplyResources(this.flowLayoutPanel2, "flowLayoutPanel2");
            this.flowLayoutPanel2.Controls.Add(this.label3);
            this.flowLayoutPanel2.Controls.Add(this.textBox_keyenter);
            this.flowLayoutPanel2.Controls.Add(this.listView1);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.toolTip1.SetToolTip(this.flowLayoutPanel2, resources.GetString("flowLayoutPanel2.ToolTip"));
            // 
            // listView1
            // 
            resources.ApplyResources(this.listView1, "listView1");
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader_Command,
            this.columnHeader_KeyBind});
            this.listView1.ContextMenuStrip = this.contextMenuStrip1;
            this.listView1.FullRowSelect = true;
            this.listView1.GridLines = true;
            this.listView1.HideSelection = false;
            this.listView1.Name = "listView1";
            this.listView1.ShowItemToolTips = true;
            this.toolTip1.SetToolTip(this.listView1, resources.GetString("listView1.ToolTip"));
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
            resources.ApplyResources(this.contextMenuStrip1, "contextMenuStrip1");
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteKeyToolStripMenuItem,
            this.addAnotherKeyToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.toolTip1.SetToolTip(this.contextMenuStrip1, resources.GetString("contextMenuStrip1.ToolTip"));
            // 
            // deleteKeyToolStripMenuItem
            // 
            resources.ApplyResources(this.deleteKeyToolStripMenuItem, "deleteKeyToolStripMenuItem");
            this.deleteKeyToolStripMenuItem.Name = "deleteKeyToolStripMenuItem";
            this.deleteKeyToolStripMenuItem.Click += new System.EventHandler(this.deleteKeyToolStripMenuItem_Click);
            // 
            // addAnotherKeyToolStripMenuItem
            // 
            resources.ApplyResources(this.addAnotherKeyToolStripMenuItem, "addAnotherKeyToolStripMenuItem");
            this.addAnotherKeyToolStripMenuItem.Name = "addAnotherKeyToolStripMenuItem";
            this.addAnotherKeyToolStripMenuItem.Click += new System.EventHandler(this.addAnotherKeyToolStripMenuItem_Click);
            // 
            // groupBox_font
            // 
            resources.ApplyResources(this.groupBox_font, "groupBox_font");
            this.groupBox_font.Controls.Add(this.flowLayoutPanel3);
            this.groupBox_font.Name = "groupBox_font";
            this.groupBox_font.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox_font, resources.GetString("groupBox_font.ToolTip"));
            // 
            // flowLayoutPanel3
            // 
            resources.ApplyResources(this.flowLayoutPanel3, "flowLayoutPanel3");
            this.flowLayoutPanel3.Controls.Add(this.label4);
            this.flowLayoutPanel3.Controls.Add(this.textBox_fontpath);
            this.flowLayoutPanel3.Controls.Add(this.button2);
            this.flowLayoutPanel3.Controls.Add(this.label5);
            this.flowLayoutPanel3.Controls.Add(this.numericUpDown_FontSize);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.toolTip1.SetToolTip(this.flowLayoutPanel3, resources.GetString("flowLayoutPanel3.ToolTip"));
            // 
            // textBox_fontpath
            // 
            resources.ApplyResources(this.textBox_fontpath, "textBox_fontpath");
            this.textBox_fontpath.Name = "textBox_fontpath";
            this.toolTip1.SetToolTip(this.textBox_fontpath, resources.GetString("textBox_fontpath.ToolTip"));
            this.textBox_fontpath.TextChanged += new System.EventHandler(this.textBox_fontpath_TextChanged);
            // 
            // button2
            // 
            resources.ApplyResources(this.button2, "button2");
            this.button2.Name = "button2";
            this.toolTip1.SetToolTip(this.button2, resources.GetString("button2.ToolTip"));
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            this.toolTip1.SetToolTip(this.label5, resources.GetString("label5.ToolTip"));
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
            this.toolTip1.SetToolTip(this.numericUpDown_FontSize, resources.GetString("numericUpDown_FontSize.ToolTip"));
            this.numericUpDown_FontSize.Value = new decimal(new int[] {
            12,
            0,
            0,
            0});
            this.numericUpDown_FontSize.ValueChanged += new System.EventHandler(this.numericUpDown_FontSize_ValueChanged);
            // 
            // groupBox_speed
            // 
            resources.ApplyResources(this.groupBox_speed, "groupBox_speed");
            this.groupBox_speed.Controls.Add(this.tableLayoutPanel2);
            this.groupBox_speed.Name = "groupBox_speed";
            this.groupBox_speed.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox_speed, resources.GetString("groupBox_speed.ToolTip"));
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.label6, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.label7, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.textBox_timeout, 0, 1);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.toolTip1.SetToolTip(this.tableLayoutPanel2, resources.GetString("tableLayoutPanel2.ToolTip"));
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            this.toolTip1.SetToolTip(this.label7, resources.GetString("label7.ToolTip"));
            // 
            // textBox_timeout
            // 
            resources.ApplyResources(this.textBox_timeout, "textBox_timeout");
            this.textBox_timeout.Name = "textBox_timeout";
            this.toolTip1.SetToolTip(this.textBox_timeout, resources.GetString("textBox_timeout.ToolTip"));
            this.textBox_timeout.TextChanged += new System.EventHandler(this.textBox_timeout_TextChanged);
            // 
            // groupBox_Screen
            // 
            resources.ApplyResources(this.groupBox_Screen, "groupBox_Screen");
            this.groupBox_Screen.Controls.Add(this.checkBox_autosize);
            this.groupBox_Screen.Name = "groupBox_Screen";
            this.groupBox_Screen.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox_Screen, resources.GetString("groupBox_Screen.ToolTip"));
            // 
            // flowLayoutPanel1
            // 
            resources.ApplyResources(this.flowLayoutPanel1, "flowLayoutPanel1");
            this.flowLayoutPanel1.Controls.Add(this.groupBox_Mouse);
            this.flowLayoutPanel1.Controls.Add(this.groupBox_keybord);
            this.flowLayoutPanel1.Controls.Add(this.groupBox_font);
            this.flowLayoutPanel1.Controls.Add(this.groupBox_speed);
            this.flowLayoutPanel1.Controls.Add(this.groupBox_Screen);
            this.flowLayoutPanel1.Controls.Add(this.button1);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.toolTip1.SetToolTip(this.flowLayoutPanel1, resources.GetString("flowLayoutPanel1.ToolTip"));
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            resources.ApplyResources(this.openFileDialog1, "openFileDialog1");
            // 
            // FormFFmoduleConfig
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.flowLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "FormFFmoduleConfig";
            this.toolTip1.SetToolTip(this, resources.GetString("$this.ToolTip"));
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormFFmoduleConfig_FormClosed);
            this.Load += new System.EventHandler(this.FormFFmoduleConfig_Load);
            this.groupBox_Mouse.ResumeLayout(false);
            this.groupBox_Mouse.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.groupBox_keybord.ResumeLayout(false);
            this.groupBox_keybord.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            this.groupBox_font.ResumeLayout(false);
            this.groupBox_font.PerformLayout();
            this.flowLayoutPanel3.ResumeLayout(false);
            this.flowLayoutPanel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown_FontSize)).EndInit();
            this.groupBox_speed.ResumeLayout(false);
            this.groupBox_speed.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.groupBox_Screen.ResumeLayout(false);
            this.groupBox_Screen.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

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
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
    }
}