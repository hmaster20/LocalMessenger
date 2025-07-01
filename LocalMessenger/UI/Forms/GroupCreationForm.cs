using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace LocalMessenger.UI.Forms
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
            InitializeComponent();
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


        //  не работал, пока не юзаем
        //private async void btnCreateGroup_Click(object sender, EventArgs e)
        //{
        //    Logger.Log("Creating group");
        //    using (var form = new GroupCreationForm())
        //    {
        //        if (form.ShowDialog() == DialogResult.OK)
        //        {
        //            var groupID = form.GroupID;
        //            var members = form.SelectedMembers;
        //            var groupKey = GenerateGroupKey();
        //            groupKeys[groupID] = groupKey;
        //            Logger.Log($"Group {groupID} created successfully with members: {string.Join(",", members)}");

        //            foreach (var member in members)
        //            {
        //                string contactIP = contactIPs.ContainsKey(member) ? contactIPs[member] : member;
        //                if (!sharedKeys.ContainsKey(member))
        //                {
        //                    Logger.Log($"No shared key found for {member}, attempting key exchange.");
        //                    var sharedKey = await ExchangeKeysWithContactAsync(contactIP);
        //                    if (sharedKey == null)
        //                    {
        //                        Logger.Log($"Failed to establish connection with {member} (IP: {contactIP})");
        //                        MessageBox.Show($"Failed to establish connection with {member}");
        //                        continue;
        //                    }
        //                }

        //                var memberSharedKey = sharedKeys[member];
        //                var nonce = GenerateNonce();
        //                var groupKeyString = Convert.ToBase64String(groupKey);
        //                var encryptedGroupKey = Encrypt(groupKeyString, memberSharedKey, nonce);
        //                var message = $"GROUP_KEY|{groupID}|{Convert.ToBase64String(encryptedGroupKey)}|{Convert.ToBase64String(nonce)}|{Convert.ToBase64String(new byte[16])}|{myLogin}";
        //                bool sent = await SendTcpMessageAsync(contactIP, message);
        //                if (!sent)
        //                {
        //                    bufferManager.AddToBuffer(contactIP, message);
        //                    Logger.Log($"Group key for {member} added to buffer due to send failure");
        //                    MessageBox.Show($"Group key for {member} added to buffer due to send failure.");
        //                }
        //            }
        //        }
        //        else
        //        {
        //            Logger.Log("Group creation cancelled");
        //        }
        //    }
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