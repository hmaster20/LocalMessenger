using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace LocalMessenger
{
    public partial class RegistrationForm : Form
    {
        public string Login { get; private set; }
        public string Name { get; private set; }
        public string Password { get; private set; } // Добавляем свойство для пароля

        private readonly string AppDataPath;

        public RegistrationForm()
        {
            InitializeComponent();
            AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalMessenger");
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLogin.Text) || string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Заполните все поля.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (txtLogin.Text.Contains(" "))
            {
                MessageBox.Show("Логин должен содержать только латинские буквы, цифры и подчеркивания. Не должен содержать пробелов!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Login = txtLogin.Text.Trim();
            Name = txtName.Text.Trim();
            Password = txtPassword.Text; // Сохраняем пароль
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}