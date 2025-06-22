namespace LocalMessenger
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListBox lstContacts;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.ComboBox cmbStatus;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.Button btnCreateGroup;
        private System.Windows.Forms.Button btnSend;

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
            this.lstContacts = new System.Windows.Forms.ListBox();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.cmbStatus = new System.Windows.Forms.ComboBox();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon();
            this.btnCreateGroup = new System.Windows.Forms.Button();
            this.btnSend = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // lstContacts
            this.lstContacts.FormattingEnabled = true;
            this.lstContacts.Location = new System.Drawing.Point(12, 12);
            this.lstContacts.Size = new System.Drawing.Size(150, 225);

            // txtMessage
            this.txtMessage.Location = new System.Drawing.Point(168, 196);
            this.txtMessage.Size = new System.Drawing.Size(200, 20);

            // cmbStatus
            this.cmbStatus.Items.AddRange(new object[] { "Онлайн", "Занят", "Не беспокоить" });
            this.cmbStatus.Location = new System.Drawing.Point(168, 222);
            this.cmbStatus.Size = new System.Drawing.Size(121, 21);

            // notifyIcon
            this.notifyIcon.Icon = new System.Drawing.Icon(SystemIcons.Application, 40, 40);
            this.notifyIcon.Text = "LocalMessenger";
            this.notifyIcon.Visible = false;

            // btnCreateGroup
            this.btnCreateGroup.Location = new System.Drawing.Point(300, 222);
            this.btnCreateGroup.Size = new System.Drawing.Size(100, 23);
            this.btnCreateGroup.Text = "Создать группу";
            this.btnCreateGroup.Click += new System.EventHandler(this.btnCreateGroup_Click);

            // btnSend
            this.btnSend.Location = new System.Drawing.Point(300, 196);
            this.btnSend.Size = new System.Drawing.Size(100, 23);
            this.btnSend.Text = "Отправить";
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);

            // MainForm
            this.ClientSize = new System.Drawing.Size(380, 255);
            this.Controls.Add(this.lstContacts);
            this.Controls.Add(this.txtMessage);
            this.Controls.Add(this.cmbStatus);
            this.Controls.Add(this.btnCreateGroup);
            this.Controls.Add(this.btnSend);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}