namespace LocalMessenger.UI.Forms
{
    partial class GroupCreationForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtGroupID;
        private System.Windows.Forms.CheckedListBox clbMembers;
        private System.Windows.Forms.Button btnCreate;

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
            this.txtGroupID = new System.Windows.Forms.TextBox();
            this.clbMembers = new System.Windows.Forms.CheckedListBox();
            this.btnCreate = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // txtGroupID
            this.txtGroupID.Location = new System.Drawing.Point(12, 12);
            this.txtGroupID.Size = new System.Drawing.Size(200, 20);

            // clbMembers
            this.clbMembers.Location = new System.Drawing.Point(12, 38);
            this.clbMembers.Size = new System.Drawing.Size(200, 100);

            // btnCreate
            this.btnCreate.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnCreate.Location = new System.Drawing.Point(137, 144);
            this.btnCreate.Size = new System.Drawing.Size(75, 23);
            this.btnCreate.Text = "Создать";
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);

            // GroupCreationForm
            this.ClientSize = new System.Drawing.Size(224, 179);
            this.Controls.Add(this.txtGroupID);
            this.Controls.Add(this.clbMembers);
            this.Controls.Add(this.btnCreate);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}