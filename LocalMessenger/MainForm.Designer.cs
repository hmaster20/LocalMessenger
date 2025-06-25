using System.Drawing;

namespace LocalMessenger
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListView lstContacts;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.ComboBox cmbStatus;
        private System.Windows.Forms.NotifyIcon notifyIcon;
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.lstContacts = new System.Windows.Forms.ListView();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.cmbStatus = new System.Windows.Forms.ComboBox();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
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
            this.SuspendLayout();
            // 
            // lstContacts
            // 
            this.lstContacts.HideSelection = false;
            this.lstContacts.Location = new System.Drawing.Point(12, 12);
            this.lstContacts.Name = "lstContacts";
            this.lstContacts.Size = new System.Drawing.Size(150, 233);
            this.lstContacts.TabIndex = 0;
            this.lstContacts.UseCompatibleStateImageBehavior = false;
            this.lstContacts.View = System.Windows.Forms.View.List;
            this.lstContacts.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.lstContacts_DrawItem);
            this.lstContacts.SelectedIndexChanged += new System.EventHandler(this.lstContacts_SelectedIndexChanged);
            // 
            // txtMessage
            // 
            this.txtMessage.Enabled = false;
            this.txtMessage.Location = new System.Drawing.Point(168, 196);
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.Size = new System.Drawing.Size(325, 24);
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
            // notifyIcon
            // 
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "LocalMessenger";
            // 
            // btnCreateGroup
            // 
            this.btnCreateGroup.Location = new System.Drawing.Point(39, 251);
            this.btnCreateGroup.Name = "btnCreateGroup";
            this.btnCreateGroup.Size = new System.Drawing.Size(100, 23);
            this.btnCreateGroup.TabIndex = 3;
            this.btnCreateGroup.Text = "Create Group";
            this.btnCreateGroup.Click += new System.EventHandler(this.btnCreateGroup_Click);
            // 
            // btnSend
            // 
            this.btnSend.Enabled = false;
            this.btnSend.Location = new System.Drawing.Point(499, 197);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(100, 23);
            this.btnSend.TabIndex = 4;
            this.btnSend.Text = "Send";
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // btnSendFile
            // 
            this.btnSendFile.Enabled = false;
            this.btnSendFile.Location = new System.Drawing.Point(499, 222);
            this.btnSendFile.Name = "btnSendFile";
            this.btnSendFile.Size = new System.Drawing.Size(100, 23);
            this.btnSendFile.TabIndex = 5;
            this.btnSendFile.Text = "Send File";
            this.btnSendFile.Click += new System.EventHandler(this.btnSendFile_Click);
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(761, 259);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(68, 23);
            this.btnExit.TabIndex = 6;
            this.btnExit.Text = "Exit";
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // btnOpenSettingsFolder
            // 
            this.btnOpenSettingsFolder.Location = new System.Drawing.Point(708, 197);
            this.btnOpenSettingsFolder.Name = "btnOpenSettingsFolder";
            this.btnOpenSettingsFolder.Size = new System.Drawing.Size(121, 23);
            this.btnOpenSettingsFolder.TabIndex = 10;
            this.btnOpenSettingsFolder.Text = "Settings Folder";
            this.btnOpenSettingsFolder.Click += new System.EventHandler(this.btnOpenSettingsFolder_Click);
            // 
            // btnDeleteAccount
            // 
            this.btnDeleteAccount.Location = new System.Drawing.Point(710, 12);
            this.btnDeleteAccount.Name = "btnDeleteAccount";
            this.btnDeleteAccount.Size = new System.Drawing.Size(119, 23);
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
            this.rtbHistory.Location = new System.Drawing.Point(168, 72);
            this.rtbHistory.Name = "rtbHistory";
            this.rtbHistory.ReadOnly = true;
            this.rtbHistory.Size = new System.Drawing.Size(661, 118);
            this.rtbHistory.TabIndex = 10;
            this.rtbHistory.Text = "";
            // 
            // btnOpenSettings
            // 
            this.btnOpenSettings.Location = new System.Drawing.Point(708, 226);
            this.btnOpenSettings.Name = "btnOpenSettings";
            this.btnOpenSettings.Size = new System.Drawing.Size(121, 23);
            this.btnOpenSettings.TabIndex = 10;
            this.btnOpenSettings.Text = "Settings";
            this.btnOpenSettings.Click += new System.EventHandler(this.btnSettings_Click);
            // 
            // btnOpenLogFile
            // 
            this.btnOpenLogFile.Location = new System.Drawing.Point(710, 42);
            this.btnOpenLogFile.Name = "btnOpenLogFile";
            this.btnOpenLogFile.Size = new System.Drawing.Size(119, 23);
            this.btnOpenLogFile.TabIndex = 12;
            this.btnOpenLogFile.Text = "Open LogFile";
            this.btnOpenLogFile.UseVisualStyleBackColor = true;
            this.btnOpenLogFile.Click += new System.EventHandler(this.btnViewLogs_Click);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(841, 294);
            this.Controls.Add(this.btnOpenLogFile);
            this.Controls.Add(this.lstContacts);
            this.Controls.Add(this.txtMessage);
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
            this.Controls.Add(this.rtbHistory);
            this.Name = "MainForm";
            this.Text = "LocalMessenger";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Button btnOpenSettings;
        private System.Windows.Forms.Button btnOpenLogFile;
    }
}