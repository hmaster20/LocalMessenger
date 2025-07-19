using System.Drawing;

namespace LocalMessenger
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListView lstContacts;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.ComboBox cmbStatus;
        private System.Windows.Forms.Button btnCreateGroup;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Button btnSendFile;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Button btnOpenSettingsFolder;
        private System.Windows.Forms.Button btnDeleteAccount;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblIP;
        private System.Windows.Forms.Label lblUserInfo;
        private System.Windows.Forms.RichTextBox rtbHistory;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lstContacts = new System.Windows.Forms.ListView();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.cmbStatus = new System.Windows.Forms.ComboBox();
            this.btnCreateGroup = new System.Windows.Forms.Button();
            this.btnSend = new System.Windows.Forms.Button();
            this.btnSendFile = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.btnOpenSettingsFolder = new System.Windows.Forms.Button();
            this.btnDeleteAccount = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblIP = new System.Windows.Forms.Label();
            this.lblUserInfo = new System.Windows.Forms.Label();
            this.rtbHistory = new System.Windows.Forms.RichTextBox();
            this.btnOpenSettings = new System.Windows.Forms.Button();
            this.btnOpenLogFile = new System.Windows.Forms.Button();
            this.sContainer = new System.Windows.Forms.SplitContainer();
            ((System.ComponentModel.ISupportInitialize)(this.sContainer)).BeginInit();
            this.sContainer.Panel1.SuspendLayout();
            this.sContainer.Panel2.SuspendLayout();
            this.sContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // lstContacts
            // 
            this.lstContacts.Alignment = System.Windows.Forms.ListViewAlignment.Left;
            this.lstContacts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstContacts.HideSelection = false;
            this.lstContacts.Location = new System.Drawing.Point(0, 0);
            this.lstContacts.Name = "lstContacts";
            this.lstContacts.Size = new System.Drawing.Size(253, 270);
            this.lstContacts.TabIndex = 0;
            this.lstContacts.UseCompatibleStateImageBehavior = false;
            this.lstContacts.View = System.Windows.Forms.View.List;
            this.lstContacts.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.lstContacts_DrawItem);
            this.lstContacts.SelectedIndexChanged += new System.EventHandler(this.lstContacts_SelectedIndexChanged);
            // 
            // txtMessage
            // 
            this.txtMessage.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.txtMessage.Enabled = false;
            this.txtMessage.Location = new System.Drawing.Point(0, 210);
            this.txtMessage.Multiline = true;
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.Size = new System.Drawing.Size(507, 60);
            this.txtMessage.TabIndex = 1;
            this.txtMessage.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtMessage_KeyPress);
            // 
            // cmbStatus
            // 
            this.cmbStatus.Items.AddRange(new object[] {
            "Online",
            "Busy",
            "Do Not Disturb"});
            this.cmbStatus.Location = new System.Drawing.Point(295, 9);
            this.cmbStatus.Name = "cmbStatus";
            this.cmbStatus.Size = new System.Drawing.Size(121, 24);
            this.cmbStatus.TabIndex = 2;
            this.cmbStatus.SelectedIndexChanged += new System.EventHandler(this.cmbStatus_SelectedIndexChanged);
            // 
            // btnCreateGroup
            // 
            this.btnCreateGroup.Location = new System.Drawing.Point(836, 206);
            this.btnCreateGroup.Name = "btnCreateGroup";
            this.btnCreateGroup.Size = new System.Drawing.Size(121, 23);
            this.btnCreateGroup.TabIndex = 3;
            this.btnCreateGroup.Text = "Create Group";
            this.btnCreateGroup.Click += new System.EventHandler(this.btnCreateGroup_Click);
            // 
            // btnSend
            // 
            this.btnSend.Enabled = false;
            this.btnSend.Location = new System.Drawing.Point(792, 287);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(100, 23);
            this.btnSend.TabIndex = 4;
            this.btnSend.Text = "Send";
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // btnSendFile
            // 
            this.btnSendFile.Enabled = false;
            this.btnSendFile.Location = new System.Drawing.Point(853, 316);
            this.btnSendFile.Name = "btnSendFile";
            this.btnSendFile.Size = new System.Drawing.Size(100, 23);
            this.btnSendFile.TabIndex = 5;
            this.btnSendFile.Text = "Send File";
            this.btnSendFile.Click += new System.EventHandler(this.btnSendFile_Click);
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(35, 26);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(68, 26);
            this.btnExit.TabIndex = 6;
            this.btnExit.Text = "Exit";
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // btnOpenSettingsFolder
            // 
            this.btnOpenSettingsFolder.Location = new System.Drawing.Point(836, 114);
            this.btnOpenSettingsFolder.Name = "btnOpenSettingsFolder";
            this.btnOpenSettingsFolder.Size = new System.Drawing.Size(121, 26);
            this.btnOpenSettingsFolder.TabIndex = 10;
            this.btnOpenSettingsFolder.Text = "Settings Folder";
            this.btnOpenSettingsFolder.Click += new System.EventHandler(this.btnOpenSettingsFolder_Click);
            // 
            // btnDeleteAccount
            // 
            this.btnDeleteAccount.Location = new System.Drawing.Point(838, 50);
            this.btnDeleteAccount.Name = "btnDeleteAccount";
            this.btnDeleteAccount.Size = new System.Drawing.Size(119, 26);
            this.btnDeleteAccount.TabIndex = 11;
            this.btnDeleteAccount.Text = "Delete Account";
            this.btnDeleteAccount.Click += new System.EventHandler(this.btnDeleteAccount_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.Location = new System.Drawing.Point(168, 12);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(200, 20);
            this.lblStatus.TabIndex = 7;
            this.lblStatus.Text = "Status: Online";
            // 
            // lblIP
            // 
            this.lblIP.Location = new System.Drawing.Point(168, 32);
            this.lblIP.Name = "lblIP";
            this.lblIP.Size = new System.Drawing.Size(200, 20);
            this.lblIP.TabIndex = 8;
            this.lblIP.Text = "IP: 0.0.0.0";
            // 
            // lblUserInfo
            // 
            this.lblUserInfo.Location = new System.Drawing.Point(168, 52);
            this.lblUserInfo.Name = "lblUserInfo";
            this.lblUserInfo.Size = new System.Drawing.Size(200, 20);
            this.lblUserInfo.TabIndex = 9;
            this.lblUserInfo.Text = "User: ";
            // 
            // rtbHistory
            // 
            this.rtbHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbHistory.Location = new System.Drawing.Point(0, 0);
            this.rtbHistory.Name = "rtbHistory";
            this.rtbHistory.ReadOnly = true;
            this.rtbHistory.Size = new System.Drawing.Size(507, 210);
            this.rtbHistory.TabIndex = 10;
            this.rtbHistory.Text = "";
            // 
            // btnOpenSettings
            // 
            this.btnOpenSettings.Location = new System.Drawing.Point(836, 146);
            this.btnOpenSettings.Name = "btnOpenSettings";
            this.btnOpenSettings.Size = new System.Drawing.Size(121, 26);
            this.btnOpenSettings.TabIndex = 10;
            this.btnOpenSettings.Text = "Settings";
            this.btnOpenSettings.Click += new System.EventHandler(this.btnSettings_Click);
            // 
            // btnOpenLogFile
            // 
            this.btnOpenLogFile.Location = new System.Drawing.Point(836, 82);
            this.btnOpenLogFile.Name = "btnOpenLogFile";
            this.btnOpenLogFile.Size = new System.Drawing.Size(119, 26);
            this.btnOpenLogFile.TabIndex = 12;
            this.btnOpenLogFile.Text = "Open LogFile";
            this.btnOpenLogFile.UseVisualStyleBackColor = true;
            this.btnOpenLogFile.Click += new System.EventHandler(this.btnViewLogs_Click);
            // 
            // sContainer
            // 
            this.sContainer.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.sContainer.Location = new System.Drawing.Point(14, 75);
            this.sContainer.Name = "sContainer";
            // 
            // sContainer.Panel1
            // 
            this.sContainer.Panel1.Controls.Add(this.lstContacts);
            // 
            // sContainer.Panel2
            // 
            this.sContainer.Panel2.Controls.Add(this.rtbHistory);
            this.sContainer.Panel2.Controls.Add(this.txtMessage);
            this.sContainer.Size = new System.Drawing.Size(772, 274);
            this.sContainer.SplitterDistance = 257;
            this.sContainer.TabIndex = 13;
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(965, 357);
            this.Controls.Add(this.sContainer);
            this.Controls.Add(this.btnOpenLogFile);
            this.Controls.Add(this.cmbStatus);
            this.Controls.Add(this.btnCreateGroup);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.btnSendFile);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnOpenSettings);
            this.Controls.Add(this.btnOpenSettingsFolder);
            this.Controls.Add(this.btnDeleteAccount);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblIP);
            this.Controls.Add(this.lblUserInfo);
            this.Name = "MainForm";
            this.Text = "LocalMessenger";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.sContainer.Panel1.ResumeLayout(false);
            this.sContainer.Panel2.ResumeLayout(false);
            this.sContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sContainer)).EndInit();
            this.sContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.Button btnOpenSettings;
        private System.Windows.Forms.Button btnOpenLogFile;
        private System.Windows.Forms.SplitContainer sContainer;
    }
}