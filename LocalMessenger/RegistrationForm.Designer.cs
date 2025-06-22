namespace LocalMessenger
{
    partial class RegistrationForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtLogin;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Button btnOK;

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
            this.btnOK = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // txtLogin
            this.txtLogin.Location = new System.Drawing.Point(12, 12);
            this.txtLogin.Size = new System.Drawing.Size(200, 20);

            // txtName
            this.txtName.Location = new System.Drawing.Point(12, 38);
            this.txtName.Size = new System.Drawing.Size(200, 20);

            // btnOK
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(137, 64);
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.Text = "OK";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);

            // RegistrationForm
            this.ClientSize = new System.Drawing.Size(224, 99);
            this.Controls.Add(this.txtLogin);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.btnOK);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}