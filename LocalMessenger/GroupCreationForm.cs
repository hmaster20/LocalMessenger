using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace LocalMessenger
{
    public partial class GroupCreationForm : Form
    {
        public string GroupID { get; private set; }
        public List<string> SelectedMembers { get; private set; }

        public GroupCreationForm()
        {
            InitializeComponent();
            // Загрузить список контактов из MainForm
        }

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