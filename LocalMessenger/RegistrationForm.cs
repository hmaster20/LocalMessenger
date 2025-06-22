using System;
using System.Windows.Forms;

namespace LocalMessenger
{
    public partial class RegistrationForm : Form
    {
        public string Login { get; private set; }
        public string Name { get; private set; }

        private TextBox txtLogin;
        private TextBox txtName;
        private Button btnOK;

        public RegistrationForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.txtLogin = new TextBox();
            this.txtName = new TextBox();
            this.btnOK = new Button();

            this.SuspendLayout();
            
            // txtLogin
            this.txtLogin.Location = new System.Drawing.Point(12, 12);
            this.txtLogin.Size = new System.Drawing.Size(200, 20);
            
            // txtName
            this.txtName.Location = new System.Drawing.Point(12, 38);
            this.txtName.Size = new System.Drawing.Size(200, 20);
            
            // btnOK
            this.btnOK.DialogResult = DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(137, 64);
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.Text = "OK";
            this.btnOK.Click += btnOK_Click;
            
            // Form
            this.ClientSize = new System.Drawing.Size(224, 99);
            this.Controls.Add(this.txtLogin);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.btnOK);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLogin.Text) || string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Заполните все поля.");
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(txtLogin.Text, "^[a-zA-Z0-9_]+$"))
            {
                MessageBox.Show("Логин должен содержать только латинские буквы, цифры и подчеркивания.");
                return;
            }

            Login = txtLogin.Text;
            Name = txtName.Text;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}