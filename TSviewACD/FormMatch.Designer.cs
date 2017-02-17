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
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.button_AddFile = new System.Windows.Forms.Button();
            this.button_AddFolder = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.button_start = new System.Windows.Forms.Button();
            this.checkBox_MD5 = new System.Windows.Forms.CheckBox();
            this.label_info = new System.Windows.Forms.Label();
            this.button_cancel = new System.Windows.Forms.Button();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deltetItemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.button_cancel);
            this.panel1.Controls.Add(this.label_info);
            this.panel1.Controls.Add(this.checkBox_MD5);
            this.panel1.Controls.Add(this.button_start);
            this.panel1.Controls.Add(this.button_AddFolder);
            this.panel1.Controls.Add(this.button_AddFile);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(765, 100);
            this.panel1.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.listBox1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 100);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(765, 602);
            this.panel2.TabIndex = 1;
            // 
            // listBox1
            // 
            this.listBox1.ContextMenuStrip = this.contextMenuStrip1;
            this.listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox1.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(0, 0);
            this.listBox1.Name = "listBox1";
            this.listBox1.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBox1.Size = new System.Drawing.Size(765, 602);
            this.listBox1.Sorted = true;
            this.listBox1.TabIndex = 0;
            this.listBox1.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.listBox1_DrawItem);
            this.listBox1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.listBox1_KeyDown);
            // 
            // button_AddFile
            // 
            this.button_AddFile.Location = new System.Drawing.Point(25, 24);
            this.button_AddFile.Name = "button_AddFile";
            this.button_AddFile.Size = new System.Drawing.Size(111, 23);
            this.button_AddFile.TabIndex = 0;
            this.button_AddFile.Text = "Add Local File";
            this.button_AddFile.UseVisualStyleBackColor = true;
            this.button_AddFile.Click += new System.EventHandler(this.button_AddFile_Click);
            // 
            // button_AddFolder
            // 
            this.button_AddFolder.Location = new System.Drawing.Point(25, 62);
            this.button_AddFolder.Name = "button_AddFolder";
            this.button_AddFolder.Size = new System.Drawing.Size(111, 23);
            this.button_AddFolder.TabIndex = 1;
            this.button_AddFolder.Text = "Add Local Folder";
            this.button_AddFolder.UseVisualStyleBackColor = true;
            this.button_AddFolder.Click += new System.EventHandler(this.button_AddFolder_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Multiselect = true;
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
            // checkBox_MD5
            // 
            this.checkBox_MD5.AutoSize = true;
            this.checkBox_MD5.Location = new System.Drawing.Point(328, 28);
            this.checkBox_MD5.Name = "checkBox_MD5";
            this.checkBox_MD5.Size = new System.Drawing.Size(171, 16);
            this.checkBox_MD5.TabIndex = 3;
            this.checkBox_MD5.Text = "Calculate MD5 and Matching";
            this.checkBox_MD5.UseVisualStyleBackColor = true;
            // 
            // label_info
            // 
            this.label_info.AutoSize = true;
            this.label_info.Location = new System.Drawing.Point(236, 62);
            this.label_info.Name = "label_info";
            this.label_info.Size = new System.Drawing.Size(0, 12);
            this.label_info.TabIndex = 4;
            // 
            // button_cancel
            // 
            this.button_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button_cancel.Location = new System.Drawing.Point(533, 24);
            this.button_cancel.Name = "button_cancel";
            this.button_cancel.Size = new System.Drawing.Size(75, 23);
            this.button_cancel.TabIndex = 5;
            this.button_cancel.Text = "Cancel";
            this.button_cancel.UseVisualStyleBackColor = true;
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
            // FormMatch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(765, 702);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "FormMatch";
            this.Text = "FormMatch";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormMatch_FormClosing);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button button_AddFolder;
        private System.Windows.Forms.Button button_AddFile;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Button button_cancel;
        private System.Windows.Forms.Label label_info;
        private System.Windows.Forms.CheckBox checkBox_MD5;
        private System.Windows.Forms.Button button_start;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem deltetItemToolStripMenuItem;
    }
}