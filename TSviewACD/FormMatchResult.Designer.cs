namespace TSviewACD
{
    partial class FormMatchResult
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.listBox_LocalOnly = new System.Windows.Forms.ListBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.button_Upload = new System.Windows.Forms.Button();
            this.button_SaveLocalList = new System.Windows.Forms.Button();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.listBox_RemoteOnly = new System.Windows.Forms.ListBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.button_trash = new System.Windows.Forms.Button();
            this.button_Download = new System.Windows.Forms.Button();
            this.button_SaveRemoteList = new System.Windows.Forms.Button();
            this.splitter2 = new System.Windows.Forms.Splitter();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.listView_Unmatch = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader9 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader10 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader11 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panel5 = new System.Windows.Forms.Panel();
            this.button_SaveUnmatchList = new System.Windows.Forms.Button();
            this.splitter3 = new System.Windows.Forms.Splitter();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.listView_Match = new System.Windows.Forms.ListView();
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panel4 = new System.Windows.Forms.Panel();
            this.button_SaveMatchedList = new System.Windows.Forms.Button();
            this.splitter4 = new System.Windows.Forms.Splitter();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.treeView_localDup = new System.Windows.Forms.TreeView();
            this.panel7 = new System.Windows.Forms.Panel();
            this.button_SaveLocalDupList = new System.Windows.Forms.Button();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.treeView_remoteDup = new System.Windows.Forms.TreeView();
            this.panel6 = new System.Windows.Forms.Panel();
            this.button_SaveRemoteDupList = new System.Windows.Forms.Button();
            this.splitter5 = new System.Windows.Forms.Splitter();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.groupBox1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.panel2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.panel5.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.panel4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.panel7.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.panel6.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.listBox_LocalOnly);
            this.groupBox1.Controls.Add(this.panel1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(832, 123);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Local Only Files";
            // 
            // listBox_LocalOnly
            // 
            this.listBox_LocalOnly.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox_LocalOnly.FormattingEnabled = true;
            this.listBox_LocalOnly.ItemHeight = 12;
            this.listBox_LocalOnly.Location = new System.Drawing.Point(3, 15);
            this.listBox_LocalOnly.Name = "listBox_LocalOnly";
            this.listBox_LocalOnly.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBox_LocalOnly.Size = new System.Drawing.Size(736, 105);
            this.listBox_LocalOnly.TabIndex = 1;
            this.listBox_LocalOnly.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.listBox_LocalOnly_Format);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.button_Upload);
            this.panel1.Controls.Add(this.button_SaveLocalList);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel1.Location = new System.Drawing.Point(739, 15);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(90, 105);
            this.panel1.TabIndex = 0;
            // 
            // button_Upload
            // 
            this.button_Upload.Location = new System.Drawing.Point(6, 32);
            this.button_Upload.Name = "button_Upload";
            this.button_Upload.Size = new System.Drawing.Size(75, 23);
            this.button_Upload.TabIndex = 1;
            this.button_Upload.Text = "Upload";
            this.button_Upload.UseVisualStyleBackColor = true;
            this.button_Upload.Click += new System.EventHandler(this.button_Upload_Click);
            // 
            // button_SaveLocalList
            // 
            this.button_SaveLocalList.Location = new System.Drawing.Point(6, 3);
            this.button_SaveLocalList.Name = "button_SaveLocalList";
            this.button_SaveLocalList.Size = new System.Drawing.Size(75, 23);
            this.button_SaveLocalList.TabIndex = 0;
            this.button_SaveLocalList.Text = "Save List";
            this.button_SaveLocalList.UseVisualStyleBackColor = true;
            this.button_SaveLocalList.Click += new System.EventHandler(this.button_SaveLocalList_Click);
            // 
            // splitter1
            // 
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter1.Location = new System.Drawing.Point(0, 123);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(832, 3);
            this.splitter1.TabIndex = 1;
            this.splitter1.TabStop = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.listBox_RemoteOnly);
            this.groupBox2.Controls.Add(this.panel2);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox2.Location = new System.Drawing.Point(0, 126);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(832, 130);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Remote Only Files";
            // 
            // listBox_RemoteOnly
            // 
            this.listBox_RemoteOnly.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox_RemoteOnly.FormattingEnabled = true;
            this.listBox_RemoteOnly.ItemHeight = 12;
            this.listBox_RemoteOnly.Location = new System.Drawing.Point(91, 15);
            this.listBox_RemoteOnly.Name = "listBox_RemoteOnly";
            this.listBox_RemoteOnly.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBox_RemoteOnly.Size = new System.Drawing.Size(738, 112);
            this.listBox_RemoteOnly.TabIndex = 2;
            this.listBox_RemoteOnly.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.listBox_RemoteOnly_Format);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.button_trash);
            this.panel2.Controls.Add(this.button_Download);
            this.panel2.Controls.Add(this.button_SaveRemoteList);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel2.Location = new System.Drawing.Point(3, 15);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(88, 112);
            this.panel2.TabIndex = 3;
            // 
            // button_trash
            // 
            this.button_trash.Location = new System.Drawing.Point(7, 75);
            this.button_trash.Name = "button_trash";
            this.button_trash.Size = new System.Drawing.Size(75, 23);
            this.button_trash.TabIndex = 3;
            this.button_trash.Text = "Trash";
            this.button_trash.UseVisualStyleBackColor = true;
            this.button_trash.Click += new System.EventHandler(this.button_trash_Click);
            // 
            // button_Download
            // 
            this.button_Download.Location = new System.Drawing.Point(7, 32);
            this.button_Download.Name = "button_Download";
            this.button_Download.Size = new System.Drawing.Size(75, 23);
            this.button_Download.TabIndex = 2;
            this.button_Download.Text = "Download";
            this.button_Download.UseVisualStyleBackColor = true;
            this.button_Download.Click += new System.EventHandler(this.button_Download_Click);
            // 
            // button_SaveRemoteList
            // 
            this.button_SaveRemoteList.Location = new System.Drawing.Point(7, 3);
            this.button_SaveRemoteList.Name = "button_SaveRemoteList";
            this.button_SaveRemoteList.Size = new System.Drawing.Size(75, 23);
            this.button_SaveRemoteList.TabIndex = 1;
            this.button_SaveRemoteList.Text = "Save List";
            this.button_SaveRemoteList.UseVisualStyleBackColor = true;
            this.button_SaveRemoteList.Click += new System.EventHandler(this.button_SaveRemoteList_Click);
            // 
            // splitter2
            // 
            this.splitter2.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter2.Location = new System.Drawing.Point(0, 256);
            this.splitter2.Name = "splitter2";
            this.splitter2.Size = new System.Drawing.Size(832, 3);
            this.splitter2.TabIndex = 3;
            this.splitter2.TabStop = false;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.listView_Unmatch);
            this.groupBox3.Controls.Add(this.panel5);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox3.Location = new System.Drawing.Point(0, 259);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(832, 182);
            this.groupBox3.TabIndex = 4;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Both but Unmatch Files";
            // 
            // listView_Unmatch
            // 
            this.listView_Unmatch.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader7,
            this.columnHeader8,
            this.columnHeader9,
            this.columnHeader10,
            this.columnHeader11});
            this.listView_Unmatch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView_Unmatch.Location = new System.Drawing.Point(91, 15);
            this.listView_Unmatch.Name = "listView_Unmatch";
            this.listView_Unmatch.Size = new System.Drawing.Size(738, 164);
            this.listView_Unmatch.TabIndex = 3;
            this.listView_Unmatch.UseCompatibleStateImageBehavior = false;
            this.listView_Unmatch.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Local";
            this.columnHeader1.Width = 200;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "LocalSize";
            this.columnHeader7.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader7.Width = 80;
            // 
            // columnHeader8
            // 
            this.columnHeader8.Text = "LocalMD5";
            // 
            // columnHeader9
            // 
            this.columnHeader9.Text = "RemoteMD5";
            // 
            // columnHeader10
            // 
            this.columnHeader10.Text = "RemoteSize";
            this.columnHeader10.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader10.Width = 80;
            // 
            // columnHeader11
            // 
            this.columnHeader11.Text = "Remote";
            this.columnHeader11.Width = 200;
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.button_SaveUnmatchList);
            this.panel5.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel5.Location = new System.Drawing.Point(3, 15);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(88, 164);
            this.panel5.TabIndex = 2;
            // 
            // button_SaveUnmatchList
            // 
            this.button_SaveUnmatchList.Location = new System.Drawing.Point(7, 3);
            this.button_SaveUnmatchList.Name = "button_SaveUnmatchList";
            this.button_SaveUnmatchList.Size = new System.Drawing.Size(75, 23);
            this.button_SaveUnmatchList.TabIndex = 2;
            this.button_SaveUnmatchList.Text = "Save List";
            this.button_SaveUnmatchList.UseVisualStyleBackColor = true;
            this.button_SaveUnmatchList.Click += new System.EventHandler(this.button_SaveUnmatchList_Click);
            // 
            // splitter3
            // 
            this.splitter3.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter3.Location = new System.Drawing.Point(0, 441);
            this.splitter3.Name = "splitter3";
            this.splitter3.Size = new System.Drawing.Size(832, 3);
            this.splitter3.TabIndex = 5;
            this.splitter3.TabStop = false;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.listView_Match);
            this.groupBox4.Controls.Add(this.panel4);
            this.groupBox4.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.groupBox4.Location = new System.Drawing.Point(0, 586);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(832, 161);
            this.groupBox4.TabIndex = 6;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Both Matched Files";
            // 
            // listView_Match
            // 
            this.listView_Match.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6});
            this.listView_Match.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView_Match.FullRowSelect = true;
            this.listView_Match.Location = new System.Drawing.Point(3, 15);
            this.listView_Match.Name = "listView_Match";
            this.listView_Match.Size = new System.Drawing.Size(736, 143);
            this.listView_Match.TabIndex = 4;
            this.listView_Match.UseCompatibleStateImageBehavior = false;
            this.listView_Match.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Local";
            this.columnHeader3.Width = 200;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Remote";
            this.columnHeader4.Width = 200;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Size";
            this.columnHeader5.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader5.Width = 80;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "MD5";
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.button_SaveMatchedList);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel4.Location = new System.Drawing.Point(739, 15);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(90, 143);
            this.panel4.TabIndex = 1;
            // 
            // button_SaveMatchedList
            // 
            this.button_SaveMatchedList.Location = new System.Drawing.Point(6, 3);
            this.button_SaveMatchedList.Name = "button_SaveMatchedList";
            this.button_SaveMatchedList.Size = new System.Drawing.Size(75, 23);
            this.button_SaveMatchedList.TabIndex = 5;
            this.button_SaveMatchedList.Text = "Save List";
            this.button_SaveMatchedList.UseVisualStyleBackColor = true;
            this.button_SaveMatchedList.Click += new System.EventHandler(this.button_SaveMatchedList_Click);
            // 
            // splitter4
            // 
            this.splitter4.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitter4.Location = new System.Drawing.Point(0, 583);
            this.splitter4.Name = "splitter4";
            this.splitter4.Size = new System.Drawing.Size(832, 3);
            this.splitter4.TabIndex = 7;
            this.splitter4.TabStop = false;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.treeView_localDup);
            this.groupBox5.Controls.Add(this.panel7);
            this.groupBox5.Dock = System.Windows.Forms.DockStyle.Left;
            this.groupBox5.Location = new System.Drawing.Point(0, 444);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(391, 139);
            this.groupBox5.TabIndex = 8;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Local Duplicate Files";
            // 
            // treeView_localDup
            // 
            this.treeView_localDup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView_localDup.Location = new System.Drawing.Point(91, 15);
            this.treeView_localDup.Name = "treeView_localDup";
            this.treeView_localDup.Size = new System.Drawing.Size(297, 121);
            this.treeView_localDup.TabIndex = 0;
            // 
            // panel7
            // 
            this.panel7.Controls.Add(this.button_SaveLocalDupList);
            this.panel7.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel7.Location = new System.Drawing.Point(3, 15);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(88, 121);
            this.panel7.TabIndex = 12;
            // 
            // button_SaveLocalDupList
            // 
            this.button_SaveLocalDupList.Location = new System.Drawing.Point(7, 3);
            this.button_SaveLocalDupList.Name = "button_SaveLocalDupList";
            this.button_SaveLocalDupList.Size = new System.Drawing.Size(75, 23);
            this.button_SaveLocalDupList.TabIndex = 3;
            this.button_SaveLocalDupList.Text = "Save List";
            this.button_SaveLocalDupList.UseVisualStyleBackColor = true;
            this.button_SaveLocalDupList.Click += new System.EventHandler(this.button_SaveLocalDupList_Click);
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.treeView_remoteDup);
            this.groupBox6.Controls.Add(this.panel6);
            this.groupBox6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox6.Location = new System.Drawing.Point(391, 444);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(441, 139);
            this.groupBox6.TabIndex = 9;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "Remote Duplicate Files";
            // 
            // treeView_remoteDup
            // 
            this.treeView_remoteDup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView_remoteDup.Location = new System.Drawing.Point(3, 15);
            this.treeView_remoteDup.Name = "treeView_remoteDup";
            this.treeView_remoteDup.Size = new System.Drawing.Size(345, 121);
            this.treeView_remoteDup.TabIndex = 0;
            // 
            // panel6
            // 
            this.panel6.Controls.Add(this.button_SaveRemoteDupList);
            this.panel6.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel6.Location = new System.Drawing.Point(348, 15);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(90, 121);
            this.panel6.TabIndex = 11;
            // 
            // button_SaveRemoteDupList
            // 
            this.button_SaveRemoteDupList.Location = new System.Drawing.Point(6, 3);
            this.button_SaveRemoteDupList.Name = "button_SaveRemoteDupList";
            this.button_SaveRemoteDupList.Size = new System.Drawing.Size(75, 23);
            this.button_SaveRemoteDupList.TabIndex = 4;
            this.button_SaveRemoteDupList.Text = "Save List";
            this.button_SaveRemoteDupList.UseVisualStyleBackColor = true;
            this.button_SaveRemoteDupList.Click += new System.EventHandler(this.button_SaveRemoteDupList_Click);
            // 
            // splitter5
            // 
            this.splitter5.Location = new System.Drawing.Point(391, 444);
            this.splitter5.Name = "splitter5";
            this.splitter5.Size = new System.Drawing.Size(3, 139);
            this.splitter5.TabIndex = 10;
            this.splitter5.TabStop = false;
            // 
            // FormMatchResult
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(832, 747);
            this.Controls.Add(this.splitter5);
            this.Controls.Add(this.groupBox6);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.splitter4);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.splitter3);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.splitter2);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.groupBox1);
            this.Name = "FormMatchResult";
            this.Text = "FormMatchResult";
            this.groupBox1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.panel7.ResumeLayout(false);
            this.groupBox6.ResumeLayout(false);
            this.panel6.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListBox listBox_LocalOnly;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.ListBox listBox_RemoteOnly;
        private System.Windows.Forms.Splitter splitter2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Splitter splitter3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.ListView listView_Unmatch;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.ColumnHeader columnHeader10;
        private System.Windows.Forms.ColumnHeader columnHeader9;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.ListView listView_Match;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ColumnHeader columnHeader11;
        private System.Windows.Forms.Splitter splitter4;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.TreeView treeView_localDup;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.TreeView treeView_remoteDup;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Splitter splitter5;
        private System.Windows.Forms.Button button_SaveLocalList;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.Button button_SaveRemoteList;
        private System.Windows.Forms.Button button_SaveUnmatchList;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.Button button_SaveLocalDupList;
        private System.Windows.Forms.Button button_SaveRemoteDupList;
        private System.Windows.Forms.Button button_SaveMatchedList;
        private System.Windows.Forms.Button button_Upload;
        private System.Windows.Forms.Button button_Download;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Button button_trash;
    }
}