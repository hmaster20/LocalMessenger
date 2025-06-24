namespace LocalMessenger
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        //private void InitializeComponent()
        //{
        //    this.Size = new Size(500, 400);
        //    this.Text = "Settings";
        //    this.FormBorderStyle = FormBorderStyle.FixedDialog;
        //    this.MaximizeBox = false;

        //    var lblLogin = new Label { Text = "Login:", Location = new Point(10, 10), Width = 100 };
        //    txtLogin = new TextBox { Location = new Point(110, 10), Width = 200 };
        //    var lblName = new Label { Text = "Name:", Location = new Point(10, 40), Width = 100 };
        //    txtName = new TextBox { Location = new Point(110, 40), Width = 200 };
        //    var lblInterface = new Label { Text = "Network Interface:", Location = new Point(10, 70), Width = 100 };
        //    cmbInterfaces = new ComboBox { Location = new Point(110, 70), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        //    var chkLiveLogs = new CheckBox { Text = "Show Live Logs", Location = new Point(10, 100), Width = 100 };
        //    txtLogs = new TextBox { Location = new Point(10, 130), Size = new Size(460, 150), Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
        //    var btnOpenLogs = new Button { Text = "Open Log File", Location = new Point(10, 290), Width = 100 };
        //    var btnSave = new Button { Text = "Save", Location = new Point(370, 290), Width = 100 };

        //    chkLiveLogs.CheckedChanged += (s, e) => txtLogs.Enabled = chkLiveLogs.Checked;
        //    btnOpenLogs.Click += btnOpenLogs_Click;
        //    btnSave.Click += btnSave_Click;

        //    this.Controls.AddRange(new Control[] { lblLogin, txtLogin, lblName, txtName, lblInterface, cmbInterfaces, chkLiveLogs, txtLogs, btnOpenLogs, btnSave });
        //}

        private TextBox txtLogin;
        private TextBox txtName;
        private ComboBox cmbInterfaces;
        private TextBox txtLogs;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
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

            // txtLogin
            this.txtLogin.Location = new System.Drawing.Point(110, 12);
            this.txtLogin.Name = "txtLogin";
            this.txtLogin.Size = new System.Drawing.Size(200, 20);
            this.txtLogin.TabIndex = 0;

            // txtName
            this.txtName.Location = new System.Drawing.Point(110, 38);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(200, 20);
            this.txtName.TabIndex = 1;

            // cmbInterfaces
            this.cmbInterfaces.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbInterfaces.Location = new System.Drawing.Point(110, 64);
            this.cmbInterfaces.Name = "cmbInterfaces";
            this.cmbInterfaces.Size = new System.Drawing.Size(200, 21);
            this.cmbInterfaces.TabIndex = 2;

            // txtLogs
            this.txtLogs.Location = new System.Drawing.Point(10, 90);
            this.txtLogs.Multiline = true;
            this.txtLogs.Name = "txtLogs";
            this.txtLogs.ReadOnly = true;
            this.txtLogs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLogs.Size = new System.Drawing.Size(460, 150);
            this.txtLogs.TabIndex = 3;

            // lblLogin
            this.lblLogin.Location = new System.Drawing.Point(10, 12);
            this.lblLogin.Name = "lblLogin";
            this.lblLogin.Size = new System.Drawing.Size(100, 23);
            this.lblLogin.Text = "Login:";

            // lblName
            this.lblName.Location = new System.Drawing.Point(10, 38);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(100, 23);
            this.lblName.Text = "Name:";

            // lblInterface
            this.lblInterface.Location = new System.Drawing.Point(10, 64);
            this.lblInterface.Name = "lblInterface";
            this.lblInterface.Size = new System.Drawing.Size(100, 23);
            this.lblInterface.Text = "Network Interface:";

            // chkLiveLogs
            this.chkLiveLogs.Location = new System.Drawing.Point(10, 246);
            this.chkLiveLogs.Name = "chkLiveLogs";
            this.chkLiveLogs.Size = new System.Drawing.Size(100, 24);
            this.chkLiveLogs.Text = "Show Live Logs";
            this.chkLiveLogs.CheckedChanged += new System.EventHandler(this.chkLiveLogs_CheckedChanged);

            // btnOpenLogs
            this.btnOpenLogs.Location = new System.Drawing.Point(10, 276);
            this.btnOpenLogs.Name = "btnOpenLogs";
            this.btnOpenLogs.Size = new System.Drawing.Size(100, 23);
            this.btnOpenLogs.Text = "Open Log File";
            this.btnOpenLogs.Click += new System.EventHandler(this.btnOpenLogs_Click);

            // btnSave
            this.btnSave.Location = new System.Drawing.Point(370, 276);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(100, 23);
            this.btnSave.Text = "Save";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            // SettingsForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(480, 310);
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