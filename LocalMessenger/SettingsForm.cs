using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;

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

        private void LoadInterfaces()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(n => new
                {
                    Name = n.Name,
                    IPs = n.GetIPProperties().UnicastAddresses
                        .Where(ip => ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .Select(ip => ip.Address.ToString()).ToList()
                })
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
            if (!File.Exists(_logFile)) return;

            const int visibleLines = 150; // Lines visible on screen + buffer
            const int blockSize = 300; // Load 300 lines at a time
            var logCache = new List<string>();
            long lastPosition = 0;

            // Initial load
            LoadLogBlock(_logFile, ref logCache, visibleLines);

            // Update UI
            txtLogs.Text = string.Join(Environment.NewLine, logCache);

            // Scroll event handler for loading more logs
            txtLogs.MouseWheel += async (s, e) =>
            {
                if (txtLogs.SelectionStart == 0 && e.Delta > 0 && logCache.Count >= visibleLines)
                {
                    await LoadMoreLogsAsync(_logFile, ref logCache, ref lastPosition, blockSize);
                    txtLogs.Text = string.Join(Environment.NewLine, logCache.Take(visibleLines));
                    txtLogs.SelectionStart = 0;
                }
            };

            // Real-time monitoring
            var timer = new Timer { Interval = 1000 };
            timer.Tick += async (s, e) =>
            {
                if (txtLogs.Enabled && File.Exists(_logFile))
                {
                    await UpdateLogsAsync(_logFile, ref logCache, ref lastPosition);
                    txtLogs.Text = string.Join(Environment.NewLine, logCache.Take(visibleLines));
                    txtLogs.SelectionStart = txtLogs.Text.Length;
                    txtLogs.ScrollToCaret();
                }
            };
            timer.Start();
        }

        private void LoadLogBlock(string logFile, ref List<string> logCache, int linesToLoad)
        {
            using (var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                for (int i = 0; i < linesToLoad && !sr.EndOfStream; i++)
                {
                    var line = sr.ReadLine();
                    if (line != null) logCache.Add(line);
                }
            }
        }

        private async Task LoadMoreLogsAsync(string logFile, ref List<string> logCache, ref long lastPosition, int blockSize)
        {
            using (var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                sr.BaseStream.Seek(0, SeekOrigin.Begin);
                var newCache = new List<string>();
                int linesRead = 0;

                while (linesRead < blockSize && !sr.EndOfStream)
                {
                    var line = await sr.ReadLineAsync();
                    if (line != null)
                    {
                        newCache.Add(line);
                        linesRead++;
                    }
                }

                logCache.InsertRange(0, newCache);
                lastPosition = sr.BaseStream.Position;

                // Trim cache to prevent memory overflow
                if (logCache.Count > blockSize * 2)
                {
                    logCache.RemoveRange(blockSize * 2, logCache.Count - blockSize * 2);
                }
            }
        }

        private async Task UpdateLogsAsync(string logFile, ref List<string> logCache, ref long lastPosition)
        {
            using (var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                sr.BaseStream.Seek(lastPosition, SeekOrigin.Begin);
                while (!sr.EndOfStream)
                {
                    var line = await sr.ReadLineAsync();
                    if (line != null) logCache.Add(line);
                }
                lastPosition = sr.BaseStream.Position;

                // Trim cache
                if (logCache.Count > 450) // visibleLines + blockSize
                {
                    logCache.RemoveRange(0, logCache.Count - 450);
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