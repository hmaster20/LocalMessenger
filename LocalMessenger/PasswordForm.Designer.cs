using System.Windows.Forms;

namespace LocalMessenger
{
    partial class PasswordForm
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
            this.Text = "Enter Password";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new System.Drawing.Size(300, 150);

            var lblPassword = new Label
            {
                Text = "Password:",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(60, 20)
            };

            var txtPassword = new TextBox
            {
                Location = new System.Drawing.Point(80, 20),
                Size = new System.Drawing.Size(180, 20),
                PasswordChar = '*'
            };

            var btnOK = new Button
            {
                Text = "OK",
                Location = new System.Drawing.Point(100, 60),
                Size = new System.Drawing.Size(75, 30)
            };
            btnOK.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    MessageBox.Show("Please enter a password.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                Password = txtPassword.Text;
                DialogResult = DialogResult.OK;
                Close();
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(180, 60),
                Size = new System.Drawing.Size(75, 30)
            };
            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);
        }

        #endregion
    }
}