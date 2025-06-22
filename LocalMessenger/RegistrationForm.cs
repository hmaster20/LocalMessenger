using System;
using System.Windows.Forms;

namespace LocalMessenger
{
    public partial class RegistrationForm : Form
    {
        public string Login { get; private set; }
        public string Name { get; private set; }

        public RegistrationForm()
        {
            InitializeComponent();
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