using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace LocalMessenger
{
    public partial class SettingsForm : Form
    {
        public string NewLogin { get; private set; }
        public string NewName { get; private set; }
        public string SelectedIP { get; private set; }
        private readonly string _appDataPath;
        private readonly string _logFile;

        public SettingsForm(string currentLogin, string currentName, string appDataPath)
        {
            InitializeComponent();
            _appDataPath = appDataPath;
            _logFile = Path.Combine(_appDataPath, "logs", "log.txt");
            NewLogin = currentLogin;
            NewName = currentName;
            txtLogin.Text = currentLogin;
            txtName.Text = currentName;
            LoadInterfaces();
            StartLogMonitoring();
        }

        private void InitializeComponent()
        {
            this.Size = new Size(500, 400);
            this.Text = "Settings";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var lblLogin = new Label { Text = "Login:", Location = new Point(10, 10), Width = 100 };
            txtLogin = new TextBox { Location = new Point(110, 10), Width = 200 };
            var lblName = new Label { Text = "Name:", Location = new Point(10, 40), Width = 100 };
            txtName = new TextBox { Location = new Point(110, 40), Width = 200 };
            var lblInterface = new Label { Text = "Network Interface:", Location = new Point(10, 70), Width = 100 };
            cmbInterfaces = new ComboBox { Location = new Point(110, 70), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            var chkLiveLogs = new CheckBox { Text = "Show Live Logs", Location = new Point(10, 100), Width = 100 };
            txtLogs = new TextBox { Location = new Point(10, 130), Size = new Size(460, 150), Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            var btnOpenLogs = new Button { Text = "Open Log File", Location = new Point(10, 290), Width = 100 };
            var btnSave = new Button { Text = "Save", Location = new Point(370, 290), Width = 100 };

            chkLiveLogs.CheckedChanged += chkLiveLogs_CheckedChanged;
            btnOpenLogs.Click += btnOpenLogs_Click;
            btnSave.Click += btnSave_Click;

            this.Controls.AddRange(new Control[] { lblLogin, txtLogin, lblName, txtName, lblInterface, cmbInterfaces, chkLiveLogs, txtLogs, btnOpenLogs, btnSave });
        }

        private TextBox txtLogin;
        private TextBox txtName;
        private ComboBox cmbInterfaces;
        private TextBox txtLogs;

        private void LoadInterfaces()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(n => new { Name = n.Name, IPs = n.GetIPProperties().UnicastAddresses
                    .Where(ip => ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .Select(ip => ip.Address.ToString()).ToList() })
                .ToList();
            foreach (var ni in interfaces)
            {
                foreach (var ip in ni.IPs)
                {
                    cmbInterfaces.Items.Add($"{ni.Name}: {ip}");
                }
            }
            if (cmbInterfaces.Items.Count > 0) cmbInterfaces.SelectedIndex = 0;
        }

        private void StartLogMonitoring()
        {
            if (File.Exists(_logFile))
            {
                using (var fs = new FileStream(_logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    sr.BaseStream.Seek(0, SeekOrigin.End);
                    var timer = new Timer { Interval = 1000 };
                    timer.Tick += (s, e) =>
                    {
                        if (txtLogs.Enabled)
                        {
                            var newLog = sr.ReadToEnd();
                            if (!string.IsNullOrEmpty(newLog))
                            {
                                this.Invoke((Action)(() => txtLogs.AppendText(newLog)));
                            }
                        }
                    };
                    timer.Start();
                }
            }
        }

        private void chkLiveLogs_CheckedChanged(object sender, EventArgs e)
        {
            txtLogs.Enabled = ((CheckBox)sender).Checked;
        }

        private void btnOpenLogs_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("notepad.exe", _logFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening log file: {ex.Message}");
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLogin.Text) || string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Login and Name cannot be empty.");
                return;
            }
            NewLogin = txtLogin.Text;
            NewName = txtName.Text;
            if (cmbInterfaces.SelectedItem != null)
            {
                SelectedIP = cmbInterfaces.SelectedItem.ToString().Split(':')[1].Trim();
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}