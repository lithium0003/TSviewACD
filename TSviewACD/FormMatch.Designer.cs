namespace TSviewACD
{
    partial class FormMatch
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMatch));
            this.panel1 = new System.Windows.Forms.Panel();
            this.button_clearRemote = new System.Windows.Forms.Button();
            this.button_clearLocal = new System.Windows.Forms.Button();
            this.button_AddRemote = new System.Windows.Forms.Button();
            this.button_cancel = new System.Windows.Forms.Button();
            this.label_info = new System.Windows.Forms.Label();
            this.checkBox_MD5 = new System.Windows.Forms.CheckBox();
            this.button_start = new System.Windows.Forms.Button();
            this.button_AddFolder = new System.Windows.Forms.Button();
            this.button_AddFile = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.listBox_remote = new System.Windows.Forms.ListBox();
            this.contextMenuStrip2 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.listBox_local = new System.Windows.Forms.ListBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deltetItemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.radioButton_Tree = new System.Windows.Forms.RadioButton();
            this.radioButton_filename = new System.Windows.Forms.RadioButton();
            this.radioButton_MD5 = new System.Windows.Forms.RadioButton();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.contextMenuStrip2.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.radioButton_MD5);
            this.panel1.Controls.Add(this.radioButton_filename);
            this.panel1.Controls.Add(this.radioButton_Tree);
            this.panel1.Controls.Add(this.button_clearRemote);
            this.panel1.Controls.Add(this.button_clearLocal);
            this.panel1.Controls.Add(this.button_AddRemote);
            this.panel1.Controls.Add(this.button_cancel);
            this.panel1.Controls.Add(this.label_info);
            this.panel1.Controls.Add(this.checkBox_MD5);
            this.panel1.Controls.Add(this.button_start);
            this.panel1.Controls.Add(this.button_AddFolder);
            this.panel1.Controls.Add(this.button_AddFile);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(765, 116);
            this.panel1.TabIndex = 0;
            // 
            // button_clearRemote
            // 
            this.button_clearRemote.Location = new System.Drawing.Point(529, 77);
            this.button_clearRemote.Name = "button_clearRemote";
            this.button_clearRemote.Size = new System.Drawing.Size(85, 23);
            this.button_clearRemote.TabIndex = 9;
            this.button_clearRemote.Text = "Clear Remote";
            this.button_clearRemote.UseVisualStyleBackColor = true;
            this.button_clearRemote.Click += new System.EventHandler(this.button_clearRemote_Click);
            // 
            // button_clearLocal
            // 
            this.button_clearLocal.Location = new System.Drawing.Point(149, 77);
            this.button_clearLocal.Name = "button_clearLocal";
            this.button_clearLocal.Size = new System.Drawing.Size(75, 23);
            this.button_clearLocal.TabIndex = 8;
            this.button_clearLocal.Text = "Clear Local";
            this.button_clearLocal.UseVisualStyleBackColor = true;
            this.button_clearLocal.Click += new System.EventHandler(this.button_clearLocal_Click);
            // 
            // button_AddRemote
            // 
            this.button_AddRemote.Location = new System.Drawing.Point(642, 77);
            this.button_AddRemote.Name = "button_AddRemote";
            this.button_AddRemote.Size = new System.Drawing.Size(111, 23);
            this.button_AddRemote.TabIndex = 6;
            this.button_AddRemote.Text = "Add Remote Item";
            this.toolTip1.SetToolTip(this.button_AddRemote, "リモートの項目を追加します。\r\nフォルダが含まれている場合は、再帰的に中身を追加します。");
            this.button_AddRemote.UseVisualStyleBackColor = true;
            this.button_AddRemote.Click += new System.EventHandler(this.button_AddRemote_Click);
            // 
            // button_cancel
            // 
            this.button_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button_cancel.Location = new System.Drawing.Point(396, 24);
            this.button_cancel.Name = "button_cancel";
            this.button_cancel.Size = new System.Drawing.Size(75, 23);
            this.button_cancel.TabIndex = 5;
            this.button_cancel.Text = "Cancel";
            this.button_cancel.UseVisualStyleBackColor = true;
            this.button_cancel.Click += new System.EventHandler(this.button_cancel_Click);
            // 
            // label_info
            // 
            this.label_info.AutoSize = true;
            this.label_info.Location = new System.Drawing.Point(236, 9);
            this.label_info.Name = "label_info";
            this.label_info.Size = new System.Drawing.Size(0, 12);
            this.label_info.TabIndex = 4;
            // 
            // checkBox_MD5
            // 
            this.checkBox_MD5.AutoSize = true;
            this.checkBox_MD5.Location = new System.Drawing.Point(376, 52);
            this.checkBox_MD5.Name = "checkBox_MD5";
            this.checkBox_MD5.Size = new System.Drawing.Size(99, 16);
            this.checkBox_MD5.TabIndex = 3;
            this.checkBox_MD5.Text = "Calculate MD5";
            this.toolTip1.SetToolTip(this.checkBox_MD5, "比較する際、ローカルのファイルのMD5ハッシュを算出し\r\nリモートのファイルのMD5と比較します。\r\nチェックされていない場合は、ファイルサイズのみの比較となりま" +
        "す。");
            this.checkBox_MD5.UseVisualStyleBackColor = true;
            // 
            // button_start
            // 
            this.button_start.Location = new System.Drawing.Point(238, 24);
            this.button_start.Name = "button_start";
            this.button_start.Size = new System.Drawing.Size(75, 23);
            this.button_start.TabIndex = 2;
            this.button_start.Text = "Start";
            this.button_start.UseVisualStyleBackColor = true;
            this.button_start.Click += new System.EventHandler(this.button1_Click);
            // 
            // button_AddFolder
            // 
            this.button_AddFolder.Location = new System.Drawing.Point(12, 77);
            this.button_AddFolder.Name = "button_AddFolder";
            this.button_AddFolder.Size = new System.Drawing.Size(111, 23);
            this.button_AddFolder.TabIndex = 1;
            this.button_AddFolder.Text = "Add Local Folder";
            this.toolTip1.SetToolTip(this.button_AddFolder, "ローカルのフォルダの中身を追加します");
            this.button_AddFolder.UseVisualStyleBackColor = true;
            this.button_AddFolder.Click += new System.EventHandler(this.button_AddFolder_Click);
            // 
            // button_AddFile
            // 
            this.button_AddFile.Location = new System.Drawing.Point(12, 48);
            this.button_AddFile.Name = "button_AddFile";
            this.button_AddFile.Size = new System.Drawing.Size(111, 23);
            this.button_AddFile.TabIndex = 0;
            this.button_AddFile.Text = "Add Local File";
            this.toolTip1.SetToolTip(this.button_AddFile, "ローカルのファイルを追加します");
            this.button_AddFile.UseVisualStyleBackColor = true;
            this.button_AddFile.Click += new System.EventHandler(this.button_AddFile_Click);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.listBox_remote);
            this.panel2.Controls.Add(this.splitter1);
            this.panel2.Controls.Add(this.listBox_local);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 116);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(765, 586);
            this.panel2.TabIndex = 1;
            // 
            // listBox_remote
            // 
            this.listBox_remote.AllowDrop = true;
            this.listBox_remote.ContextMenuStrip = this.contextMenuStrip2;
            this.listBox_remote.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox_remote.FormattingEnabled = true;
            this.listBox_remote.ItemHeight = 12;
            this.listBox_remote.Location = new System.Drawing.Point(376, 0);
            this.listBox_remote.Name = "listBox_remote";
            this.listBox_remote.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBox_remote.Size = new System.Drawing.Size(389, 586);
            this.listBox_remote.Sorted = true;
            this.listBox_remote.TabIndex = 2;
            this.listBox_remote.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.listBox_remote_Format);
            this.listBox_remote.DragDrop += new System.Windows.Forms.DragEventHandler(this.listBox_remote_DragDrop);
            this.listBox_remote.DragEnter += new System.Windows.Forms.DragEventHandler(this.listBox_remote_DragEnter);
            this.listBox_remote.KeyDown += new System.Windows.Forms.KeyEventHandler(this.listBox_remote_KeyDown);
            // 
            // contextMenuStrip2
            // 
            this.contextMenuStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1});
            this.contextMenuStrip2.Name = "contextMenuStrip1";
            this.contextMenuStrip2.Size = new System.Drawing.Size(156, 26);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.toolStripMenuItem1.Size = new System.Drawing.Size(155, 22);
            this.toolStripMenuItem1.Text = "Deltet Item";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
            // 
            // splitter1
            // 
            this.splitter1.Location = new System.Drawing.Point(373, 0);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(3, 586);
            this.splitter1.TabIndex = 1;
            this.splitter1.TabStop = false;
            // 
            // listBox_local
            // 
            this.listBox_local.AllowDrop = true;
            this.listBox_local.ContextMenuStrip = this.contextMenuStrip1;
            this.listBox_local.Dock = System.Windows.Forms.DockStyle.Left;
            this.listBox_local.FormattingEnabled = true;
            this.listBox_local.ItemHeight = 12;
            this.listBox_local.Location = new System.Drawing.Point(0, 0);
            this.listBox_local.Name = "listBox_local";
            this.listBox_local.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBox_local.Size = new System.Drawing.Size(373, 586);
            this.listBox_local.Sorted = true;
            this.listBox_local.TabIndex = 0;
            this.listBox_local.DragDrop += new System.Windows.Forms.DragEventHandler(this.listBox_local_DragDrop);
            this.listBox_local.DragEnter += new System.Windows.Forms.DragEventHandler(this.listBox_local_DragEnter);
            this.listBox_local.KeyDown += new System.Windows.Forms.KeyEventHandler(this.listBox1_KeyDown);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deltetItemToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(156, 26);
            // 
            // deltetItemToolStripMenuItem
            // 
            this.deltetItemToolStripMenuItem.Name = "deltetItemToolStripMenuItem";
            this.deltetItemToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.deltetItemToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.deltetItemToolStripMenuItem.Text = "Deltet Item";
            this.deltetItemToolStripMenuItem.Click += new System.EventHandler(this.deltetItemToolStripMenuItem_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Multiselect = true;
            // 
            // radioButton_Tree
            // 
            this.radioButton_Tree.AutoSize = true;
            this.radioButton_Tree.Checked = true;
            this.radioButton_Tree.Location = new System.Drawing.Point(278, 51);
            this.radioButton_Tree.Name = "radioButton_Tree";
            this.radioButton_Tree.Size = new System.Drawing.Size(75, 16);
            this.radioButton_Tree.TabIndex = 10;
            this.radioButton_Tree.TabStop = true;
            this.radioButton_Tree.Text = "Keep Tree";
            this.radioButton_Tree.UseVisualStyleBackColor = true;
            // 
            // radioButton_filename
            // 
            this.radioButton_filename.AutoSize = true;
            this.radioButton_filename.Location = new System.Drawing.Point(278, 72);
            this.radioButton_filename.Name = "radioButton_filename";
            this.radioButton_filename.Size = new System.Drawing.Size(96, 16);
            this.radioButton_filename.TabIndex = 11;
            this.radioButton_filename.Text = "Filename Only";
            this.radioButton_filename.UseVisualStyleBackColor = true;
            // 
            // radioButton_MD5
            // 
            this.radioButton_MD5.AutoSize = true;
            this.radioButton_MD5.Location = new System.Drawing.Point(278, 94);
            this.radioButton_MD5.Name = "radioButton_MD5";
            this.radioButton_MD5.Size = new System.Drawing.Size(70, 16);
            this.radioButton_MD5.TabIndex = 12;
            this.radioButton_MD5.Text = "Use MD5";
            this.radioButton_MD5.UseVisualStyleBackColor = true;
            this.radioButton_MD5.CheckedChanged += new System.EventHandler(this.radioButton_MD5_CheckedChanged);
            // 
            // FormMatch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(765, 702);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormMatch";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FormMatch";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormMatch_FormClosing);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.contextMenuStrip2.ResumeLayout(false);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button button_AddFolder;
        private System.Windows.Forms.Button button_AddFile;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.ListBox listBox_local;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Button button_cancel;
        private System.Windows.Forms.Label label_info;
        private System.Windows.Forms.CheckBox checkBox_MD5;
        private System.Windows.Forms.Button button_start;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem deltetItemToolStripMenuItem;
        private System.Windows.Forms.Button button_AddRemote;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.ListBox listBox_remote;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button button_clearRemote;
        private System.Windows.Forms.Button button_clearLocal;
        private System.Windows.Forms.RadioButton radioButton_MD5;
        private System.Windows.Forms.RadioButton radioButton_filename;
        private System.Windows.Forms.RadioButton radioButton_Tree;
    }
}