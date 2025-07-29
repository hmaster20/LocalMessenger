namespace LocalMessenger.Forms
{
    partial class SettingsForm
    {
        private System.ComponentModel.Container components = null;

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
            this.txtLogin = new System.Windows.Forms.TextBox();
            this.txtName = new System.Windows.Forms.TextBox();
            this.cmbInterfaces = new System.Windows.Forms.ComboBox();
            this.txtLogs = new System.Windows.Forms.TextBox();
            this.lblLogin = new System.Windows.Forms.Label();
            this.lblName = new System.Windows.Forms.Label();
            this.lblInterface = new System.Windows.Forms.Label();
            this.chkLiveLogs = new System.Windows.Forms.CheckBox();
            this.btnOpenLogs = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnDeleteAccount = new System.Windows.Forms.Button();
            this.btnOpenLogFile = new System.Windows.Forms.Button();
            this.btnOpenSettings = new System.Windows.Forms.Button();
            this.btnOpenSettingsFolder = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtLogin
            // 
            this.txtLogin.Location = new System.Drawing.Point(130, 15);
            this.txtLogin.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtLogin.Name = "txtLogin";
            this.txtLogin.Size = new System.Drawing.Size(233, 24);
            this.txtLogin.TabIndex = 0;
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(130, 47);
            this.txtName.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(233, 24);
            this.txtName.TabIndex = 1;
            // 
            // cmbInterfaces
            // 
            this.cmbInterfaces.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbInterfaces.Location = new System.Drawing.Point(130, 79);
            this.cmbInterfaces.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.cmbInterfaces.Name = "cmbInterfaces";
            this.cmbInterfaces.Size = new System.Drawing.Size(233, 24);
            this.cmbInterfaces.TabIndex = 2;
            // 
            // txtLogs
            // 
            this.txtLogs.Location = new System.Drawing.Point(12, 111);
            this.txtLogs.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtLogs.Multiline = true;
            this.txtLogs.Name = "txtLogs";
            this.txtLogs.ReadOnly = true;
            this.txtLogs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLogs.Size = new System.Drawing.Size(536, 184);
            this.txtLogs.TabIndex = 3;
            // 
            // lblLogin
            // 
            this.lblLogin.Location = new System.Drawing.Point(12, 15);
            this.lblLogin.Name = "lblLogin";
            this.lblLogin.Size = new System.Drawing.Size(117, 28);
            this.lblLogin.TabIndex = 5;
            this.lblLogin.Text = "Login:";
            // 
            // lblName
            // 
            this.lblName.Location = new System.Drawing.Point(12, 47);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(117, 28);
            this.lblName.TabIndex = 4;
            this.lblName.Text = "Name:";
            // 
            // lblInterface
            // 
            this.lblInterface.Location = new System.Drawing.Point(12, 79);
            this.lblInterface.Name = "lblInterface";
            this.lblInterface.Size = new System.Drawing.Size(117, 25);
            this.lblInterface.TabIndex = 3;
            this.lblInterface.Text = "Network Interface:";
            // 
            // chkLiveLogs
            // 
            this.chkLiveLogs.Location = new System.Drawing.Point(12, 303);
            this.chkLiveLogs.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkLiveLogs.Name = "chkLiveLogs";
            this.chkLiveLogs.Size = new System.Drawing.Size(117, 30);
            this.chkLiveLogs.TabIndex = 2;
            this.chkLiveLogs.Text = "Show Live Logs";
            this.chkLiveLogs.CheckedChanged += new System.EventHandler(this.chkLiveLogs_CheckedChanged);
            // 
            // btnOpenLogs
            // 
            this.btnOpenLogs.Location = new System.Drawing.Point(12, 340);
            this.btnOpenLogs.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnOpenLogs.Name = "btnOpenLogs";
            this.btnOpenLogs.Size = new System.Drawing.Size(117, 28);
            this.btnOpenLogs.TabIndex = 1;
            this.btnOpenLogs.Text = "Open Log File";
            this.btnOpenLogs.Click += new System.EventHandler(this.btnOpenLogs_Click);
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(432, 340);
            this.btnSave.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(117, 28);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "Save";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnDeleteAccount
            // 
            this.btnDeleteAccount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDeleteAccount.Location = new System.Drawing.Point(621, 10);
            this.btnDeleteAccount.Name = "btnDeleteAccount";
            this.btnDeleteAccount.Size = new System.Drawing.Size(119, 26);
            this.btnDeleteAccount.TabIndex = 12;
            this.btnDeleteAccount.Text = "Delete Account";
            this.btnDeleteAccount.Click += new System.EventHandler(this.btnDeleteAccount_Click);
            // 
            // btnOpenLogFile
            // 
            this.btnOpenLogFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenLogFile.Location = new System.Drawing.Point(621, 47);
            this.btnOpenLogFile.Name = "btnOpenLogFile";
            this.btnOpenLogFile.Size = new System.Drawing.Size(119, 26);
            this.btnOpenLogFile.TabIndex = 15;
            this.btnOpenLogFile.Text = "Open LogFile";
            this.btnOpenLogFile.UseVisualStyleBackColor = true;
            // 
            // btnOpenSettings
            // 
            this.btnOpenSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenSettings.Location = new System.Drawing.Point(621, 111);
            this.btnOpenSettings.Name = "btnOpenSettings";
            this.btnOpenSettings.Size = new System.Drawing.Size(121, 26);
            this.btnOpenSettings.TabIndex = 13;
            this.btnOpenSettings.Text = "Settings";
            // 
            // btnOpenSettingsFolder
            // 
            this.btnOpenSettingsFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenSettingsFolder.Location = new System.Drawing.Point(621, 79);
            this.btnOpenSettingsFolder.Name = "btnOpenSettingsFolder";
            this.btnOpenSettingsFolder.Size = new System.Drawing.Size(121, 26);
            this.btnOpenSettingsFolder.TabIndex = 14;
            this.btnOpenSettingsFolder.Text = "Settings Folder";
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(781, 382);
            this.Controls.Add(this.btnOpenLogFile);
            this.Controls.Add(this.btnOpenSettings);
            this.Controls.Add(this.btnOpenSettingsFolder);
            this.Controls.Add(this.btnDeleteAccount);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnOpenLogs);
            this.Controls.Add(this.chkLiveLogs);
            this.Controls.Add(this.lblInterface);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.lblLogin);
            this.Controls.Add(this.txtLogs);
            this.Controls.Add(this.cmbInterfaces);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.txtLogin);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "SettingsForm";
            this.Text = "Settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.TextBox txtLogin;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.ComboBox cmbInterfaces;
        private System.Windows.Forms.TextBox txtLogs;
        private System.Windows.Forms.Label lblLogin;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblInterface;
        private System.Windows.Forms.CheckBox chkLiveLogs;
        private System.Windows.Forms.Button btnOpenLogs;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnDeleteAccount;
        private System.Windows.Forms.Button btnOpenLogFile;
        private System.Windows.Forms.Button btnOpenSettings;
        private System.Windows.Forms.Button btnOpenSettingsFolder;
    }
}