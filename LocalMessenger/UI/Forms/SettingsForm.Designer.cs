namespace LocalMessenger
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
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(560, 382);
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
    }
}