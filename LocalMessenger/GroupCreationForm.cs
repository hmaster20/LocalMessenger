using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace LocalMessenger
{
    public partial class GroupCreationForm : Form
    {
        public string GroupID { get; private set; }
        public List<string> SelectedMembers { get; private set; }

        // private TextBox txtGroupID;
        // private CheckedListBox clbMembers;
        // private Button btnCreate;

        public GroupCreationForm()
        {
            //InitializeComponent();
        }

        //private void InitializeComponent()
        //{
        //    this.txtGroupID = new TextBox();
        //    this.clbMembers = new CheckedListBox();
        //    this.btnCreate = new Button();

        //    this.SuspendLayout();
            
        //    // txtGroupID
        //    this.txtGroupID.Location = new System.Drawing.Point(12, 12);
        //    this.txtGroupID.Size = new System.Drawing.Size(200, 20);
            
        //    // clbMembers
        //    this.clbMembers.Location = new System.Drawing.Point(12, 38);
        //    this.clbMembers.Size = new System.Drawing.Size(200, 100);
            
        //    // btnCreate
        //    this.btnCreate.DialogResult = DialogResult.OK;
        //    this.btnCreate.Location = new System.Drawing.Point(137, 144);
        //    this.btnCreate.Size = new System.Drawing.Size(75, 23);
        //    this.btnCreate.Text = "Создать";
        //    this.btnCreate.Click += btnCreate_Click;
            
        //    // Form
        //    this.ClientSize = new System.Drawing.Size(224, 179);
        //    this.Controls.Add(this.txtGroupID);
        //    this.Controls.Add(this.clbMembers);
        //    this.Controls.Add(this.btnCreate);
        //    this.ResumeLayout(false);
        //    this.PerformLayout();
        //}

        private void btnCreate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtGroupID.Text) || clbMembers.CheckedItems.Count == 0)
            {
                MessageBox.Show("Укажите ID группы и выберите участников.");
                return;
            }

            GroupID = txtGroupID.Text;
            SelectedMembers = new List<string>();
            foreach (var item in clbMembers.CheckedItems)
            {
                SelectedMembers.Add(item.ToString());
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}