using System.Drawing;

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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.lstContacts = new System.Windows.Forms.ListBox();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.cmbStatus = new System.Windows.Forms.ComboBox();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.btnCreateGroup = new System.Windows.Forms.Button();
            this.btnSend = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lstContacts
            // 
            this.lstContacts.FormattingEnabled = true;
            this.lstContacts.ItemHeight = 16;
            this.lstContacts.Location = new System.Drawing.Point(12, 12);
            this.lstContacts.Name = "lstContacts";
            this.lstContacts.Size = new System.Drawing.Size(150, 212);
            this.lstContacts.TabIndex = 0;
            // 
            // txtMessage
            // 
            this.txtMessage.Location = new System.Drawing.Point(168, 196);
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.Size = new System.Drawing.Size(200, 24);
            this.txtMessage.TabIndex = 1;
            // 
            // cmbStatus
            // 
            this.cmbStatus.Items.AddRange(new object[] {
            "Онлайн",
            "Занят",
            "Не беспокоить"});
            this.cmbStatus.Location = new System.Drawing.Point(168, 222);
            this.cmbStatus.Name = "cmbStatus";
            this.cmbStatus.Size = new System.Drawing.Size(121, 24);
            this.cmbStatus.TabIndex = 2;
            this.cmbStatus.SelectedIndexChanged += new System.EventHandler(this.cmbStatus_SelectedIndexChanged);
            // 
            // notifyIcon
            // 
            //this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Icon = new System.Drawing.Icon(SystemIcons.Application, 40, 40);
            this.notifyIcon.Text = "LocalMessenger";
            this.notifyIcon.Visible = false;
            this.notifyIcon.DoubleClick += new System.EventHandler(this.notifyIcon_DoubleClick);








            // 
            // btnCreateGroup
            // 
            this.btnCreateGroup.Location = new System.Drawing.Point(300, 222);
            this.btnCreateGroup.Name = "btnCreateGroup";
            this.btnCreateGroup.Size = new System.Drawing.Size(100, 23);
            this.btnCreateGroup.TabIndex = 3;
            this.btnCreateGroup.Text = "Создать группу";
            this.btnCreateGroup.Click += new System.EventHandler(this.btnCreateGroup_Click);
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(374, 197);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(100, 23);
            this.btnSend.TabIndex = 4;
            this.btnSend.Text = "Отправить";
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(539, 255);
            this.Controls.Add(this.lstContacts);
            this.Controls.Add(this.txtMessage);
            this.Controls.Add(this.cmbStatus);
            this.Controls.Add(this.btnCreateGroup);
            this.Controls.Add(this.btnSend);
            this.Name = "MainForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }


        //private void InitializeComponent()
        //{
        //    // Инициализация элементов интерфейса
        //    this.lstContacts = new ListBox();
        //    this.txtMessage = new TextBox();
        //    this.cmbStatus = new ComboBox();
        //    this.notifyIcon = new NotifyIcon();
        //    this.btnSend = new Button();
        //    this.btnCreateGroup = new Button();

        //    // Настройка элементов
        //    this.SuspendLayout();

        //    // lstContacts
        //    this.lstContacts.FormattingEnabled = true;
        //    this.lstContacts.Location = new System.Drawing.Point(12, 12);
        //    this.lstContacts.Size = new System.Drawing.Size(150, 225);

        //    // txtMessage
        //    this.txtMessage.Location = new System.Drawing.Point(168, 196);
        //    this.txtMessage.Size = new System.Drawing.Size(200, 20);

        //    // btnSend
        //    this.btnSend.Location = new System.Drawing.Point(374, 196);
        //    this.btnSend.Size = new System.Drawing.Size(75, 23);
        //    this.btnSend.Text = "Отправить";
        //    this.btnSend.Click += new System.EventHandler(this.btnSend_Click);

        //    // btnCreateGroup
        //    this.btnCreateGroup.Location = new System.Drawing.Point(168, 12);
        //    this.btnCreateGroup.Size = new System.Drawing.Size(100, 23);
        //    this.btnCreateGroup.Text = "Создать группу";
        //    this.btnCreateGroup.Click += new System.EventHandler(this.btnCreateGroup_Click);

        //    // cmbStatus
        //    this.cmbStatus.Items.AddRange(new object[] { "Онлайн", "Занят", "Не беспокоить" });
        //    this.cmbStatus.Location = new System.Drawing.Point(168, 222);
        //    this.cmbStatus.Size = new System.Drawing.Size(121, 21);
        //    this.cmbStatus.SelectedIndexChanged += new System.EventHandler(this.cmbStatus_SelectedIndexChanged);

        //    // notifyIcon
        //    this.notifyIcon.Icon = new System.Drawing.Icon(SystemIcons.Application, 40, 40);
        //    this.notifyIcon.Text = "LocalMessenger";
        //    this.notifyIcon.Visible = false;
        //    this.notifyIcon.DoubleClick += new System.EventHandler(this.notifyIcon_DoubleClick);

        //    // MainForm
        //    this.ClientSize = new System.Drawing.Size(460, 255);
        //    this.Controls.Add(this.lstContacts);
        //    this.Controls.Add(this.txtMessage);
        //    this.Controls.Add(this.btnSend);
        //    this.Controls.Add(this.btnCreateGroup);
        //    this.Controls.Add(this.cmbStatus);
        //    this.Text = "LocalMessenger";
        //    this.Resize += new System.EventHandler(this.MainForm_Resize);
        //    this.ResumeLayout(false);
        //    this.PerformLayout();
        //}

    }
}