using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LocalMessenger.Core.Models;
using LocalMessenger.Core.Services;
using LocalMessenger.Utilities;
using System.Threading.Tasks;
using LocalMessenger.Core.Security;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LocalMessenger.Core.Models;
using LocalMessenger.Core.Services;
using LocalMessenger.Utilities;

namespace LocalMessenger.UI.Forms
{
    public class UIManager
    {
        private readonly Form _form;
        private readonly ListView _lstContacts;
        private readonly RichTextBox _rtbHistory;
        private readonly TextBox _txtMessage;
        private readonly Label _lblStatus;
        private readonly Label _lblIP;
        private readonly Label _lblUserInfo;
        private readonly ContactManager _contactManager;
        private readonly HistoryManager _historyManager;
        private readonly string _myLogin;
        private readonly ToolStripStatusLabel _statusLabel;
        private readonly ToolStripProgressBar _progressBar;
        private readonly Timer _contactUpdateTimer;
        private readonly NotifyIcon _notifyIcon;

        public string SelectedContact => _lstContacts.SelectedItems.Count > 0 ? _lstContacts.SelectedItems[0].Text.Split(' ')[0] : null;

        public UIManager(Form form, ListView lstContacts, RichTextBox rtbHistory, TextBox txtMessage,
            Label lblStatus, Label lblIP, Label lblUserInfo, ContactManager contactManager, HistoryManager historyManager, string myLogin)
        {
            _form = form;
            _lstContacts = lstContacts;
            _rtbHistory = rtbHistory;
            _txtMessage = txtMessage;
            _lblStatus = lblStatus;
            _lblIP = lblIP;
            _lblUserInfo = lblUserInfo;
            _contactManager = contactManager;
            _historyManager = historyManager;
            _myLogin = myLogin;
            _statusLabel = new ToolStripStatusLabel();
            _progressBar = new ToolStripProgressBar { Size = new Size(100, 16), Visible = false };
            _contactUpdateTimer = new Timer { Interval = 30000 };
            _notifyIcon = new NotifyIcon { Icon = Properties.Resources.LocalMessenger, Visible = true };
        }

        public void InitializeUI()
        {
            var statusStrip = new StatusStrip();
            statusStrip.Items.Add(_statusLabel);
            statusStrip.Items.Add(_progressBar);
            _form.Controls.Add(statusStrip);

            _rtbHistory.WordWrap = true;
            _rtbHistory.ScrollBars = RichTextBoxScrollBars.Vertical;
            _rtbHistory.Font = new Font("Segoe UI Emoji", 10, FontStyle.Regular, GraphicsUnit.Point);
            _txtMessage.Font = new Font("Segoe UI Emoji", 10, FontStyle.Regular, GraphicsUnit.Point);
            _lstContacts.OwnerDraw = true;
            _lstContacts.DrawItem += LstContacts_DrawItem;
            _rtbHistory.LinkClicked += RtbHistory_LinkClicked;
            _txtMessage.KeyDown += TxtMessage_KeyDown;
            _lstContacts.SelectedIndexChanged += LstContacts_SelectedIndexChanged;
            _contactUpdateTimer.Tick += (s, e) => _contactManager.CheckContactTimeouts();
            _contactUpdateTimer.Start();

            InitializeEmojiMenu();
            Logger.Log("UI initialized with Segoe UI Emoji font and emoji menu");
        }

        private void InitializeEmojiMenu()
        {
            var emojiMenu = new ContextMenuStrip();
            var emojis = new[] { "😊", "😂", "😍", "😘", "🥰", "🐱", "👍", "🤝", "😢", "🥂", "🤗", "😳", "🤦🏻‍", "🎂", "🍻", "🍾", "🍽", "🚀", "🌟", "❤️" };
            foreach (var emoji in emojis)
            {
                emojiMenu.Items.Add(emoji, null, (s, e) => _txtMessage.AppendText(emoji));
            }
            _txtMessage.ContextMenuStrip = emojiMenu;
            Logger.Log("Emoji menu initialized with extended emoji set");
        }

        private void RtbHistory_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                var filePath = e.LinkText;
                if (File.Exists(filePath))
                {
                    Process.Start(filePath);
                }
                else
                {
                    MessageBox.Show("File not found.", "Error");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error opening file: {ex.Message}");
            }
        }

        public void TxtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(_txtMessage.Text))
            {
                e.SuppressKeyPress = true;
                _form.Controls.OfType<Button>().FirstOrDefault(b => b.Name == "btnSend")?.PerformClick();
            }
        }

        public void LstContacts_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            if (e.ItemIndex < 0) return;
            var item = _lstContacts.Items[e.ItemIndex];
            var text = item.Text;
            var login = text.Split(' ')[0];
            var status = text.Contains("Online") ? "Online" : "Offline";
            var isBlinking = _contactManager.IsBlinkingContact(login) && (DateTime.Now.Second % 2 == 0);
            e.DrawBackground();
            using (var brush = new SolidBrush(isBlinking ? Color.Yellow : Color.Black))
            {
                e.Graphics.DrawImage(_lstContacts.SmallImageList.Images[status], e.Bounds.Left, e.Bounds.Top);
                e.Graphics.DrawString(text, new Font("Segoe UI Emoji", 9), brush, e.Bounds.Left + 20, e.Bounds.Top);
            }
            e.DrawFocusRectangle();
        }

        public void LstContacts_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSendControlsState();
            if (_lstContacts.SelectedItems.Count > 0)
            {
                var contact = _lstContacts.SelectedItems[0].Text.Split(' ')[0];
                UpdateHistoryDisplay(contact);
                _contactManager.RemoveBlinkingContact(contact);
                Logger.Log($"Selected contact: {_lstContacts.SelectedItems[0].Text}");
            }
            else
            {
                _rtbHistory.Clear();
                Logger.Log("No contact selected, cleared history display");
            }
        }

        public void UpdateHistoryDisplay(string contact)
        {
            _rtbHistory.Clear();
            var messages = _historyManager.LoadMessages(contact);
            if (messages == null || messages.Count == 0)
            {
                Logger.Log($"No history found for {contact}");
                return;
            }

            foreach (var msg in messages)
            {
                var prefix = $"[{msg.Timestamp:dd.MM.yyyy HH:mm:ss}] {msg.Sender}: ";
                switch (msg.Type)
                {
                    case MessageType.Text:
                        _rtbHistory.AppendText(prefix + msg.Content + Environment.NewLine);
                        break;
                    case MessageType.File:
                    case MessageType.Image:
                        _rtbHistory.AppendText(prefix + $"[{msg.Type}] {Path.GetFileName(msg.Content)}" + Environment.NewLine);
                        _rtbHistory.SelectionStart = _rtbHistory.TextLength - Path.GetFileName(msg.Content).Length - 1;
                        _rtbHistory.SelectionLength = Path.GetFileName(msg.Content).Length;
                        _rtbHistory.SelectionColor = Color.Blue;
                        _rtbHistory.SelectionFont = new Font(_rtbHistory.Font, FontStyle.Underline);
                        break;
                }
            }
            _rtbHistory.ScrollToCaret();
            _rtbHistory.SelectAll();
            _rtbHistory.SelectionFont = new Font("Segoe UI Emoji", 10, FontStyle.Regular, GraphicsUnit.Point);
            _rtbHistory.DeselectAll();
            Logger.Log($"Displayed history for {contact}, {messages.Count} messages loaded");
        }

        public void LoadAllHistories()
        {
            var historyFiles = Directory.GetFiles(Configuration.HistoryPath, "*.json");
            var contactsWithHistory = new HashSet<string>();

            foreach (var file in historyFiles)
            {
                var contact = Path.GetFileNameWithoutExtension(file);
                contactsWithHistory.Add(contact);
                _historyManager.LoadMessages(contact);
            }

            foreach (var contact in contactsWithHistory)
            {
                if (contact != _myLogin && !_lstContacts.Items.Cast<ListViewItem>().Any(i => i.Text.StartsWith(contact)))
                {
                    _lstContacts.Items.Add(new ListViewItem($"{contact} ({contact}, Offline)") { ImageKey = "Offline" });
                    Logger.Log($"Added contact with history to lstContacts: {contact} (Offline)");
                }
            }

            if (_lstContacts.Items.Count > 0)
            {
                _lstContacts.Items[0].Selected = true;
                _lstContacts.Select();
                Logger.Log("First contact selected automatically to display history");
            }

            Logger.Log($"Loaded {contactsWithHistory.Count} histories");
        }

        public void UpdateStatusAndIP(string myIP, string myName, string myLogin, string myStatus)
        {
            _lblStatus.Text = $"Status: {myStatus}";
            _lblIP.Text = $"IP: {myIP}";
            _lblUserInfo.Text = $"User: {myName} ({myLogin})";
            Logger.Log($"Updated UI: Status={myStatus}, IP={myIP}, User={myLogin} ({myName})");
        }

        public void UpdateSendControlsState()
        {
            var isEnabled = _lstContacts.SelectedItems.Count > 0 && !_lstContacts.SelectedItems[0].Text.StartsWith(_myLogin);
            var btnSend = _form.Controls.OfType<Button>().FirstOrDefault(b => b.Name == "btnSend");
            var btnSendFile = _form.Controls.OfType<Button>().FirstOrDefault(b => b.Name == "btnSendFile");
            if (btnSend != null) btnSend.Enabled = isEnabled;
            if (btnSendFile != null) btnSendFile.Enabled = isEnabled;
            _txtMessage.Enabled = isEnabled;
            Logger.Log($"Send controls state updated: {isEnabled}, Selected={(_lstContacts.SelectedItems.Count > 0 ? _lstContacts.SelectedItems[0].Text : "None")}");
        }

        public bool CanSendMessage()
        {
            if (_lstContacts.SelectedItems.Count == 0)
            {
                Logger.Log("No contact selected for sending message.");
                return false;
            }
            var contactLogin = _lstContacts.SelectedItems[0].Text.Split(' ')[0];
            if (contactLogin == _myLogin)
            {
                Logger.Log("Attempted to send message to self. Ignored.");
                MessageBox.Show("Cannot send message to yourself.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        public string GetMessageText()
        {
            var text = _txtMessage.Text;
            _txtMessage.Clear();
            return text;
        }

        public void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public async Task HandleSendMessageAsync(ContactManager contactManager, MessageHandler messageHandler, HistoryManager historyManager, Dictionary<string, byte[]> sharedKeys, string myLogin)
        {
            if (!CanSendMessage()) return;
            var contactLogin = SelectedContact;
            var messageText = GetMessageText();
            if (string.IsNullOrWhiteSpace(messageText)) return;

            var contactIP = contactManager.GetContactIP(contactLogin);
            var mainForm = _form.Controls.OfType<MainForm>().First();
            if (!sharedKeys.ContainsKey(contactLogin))
            {
                Logger.Log($"No shared key found for {contactLogin}, attempting key exchange.");
                var sharedKey = await mainForm.ExchangeKeysWithContactAsync(contactIP);
                if (sharedKey == null)
                {
                    Logger.Log($"Failed to establish connection with {contactLogin} (IP: {contactIP})");
                    ShowError($"Failed to establish connection with {contactLogin}");
                    return;
                }
            }

            var nonce = CryptoUtils.GenerateNonce();
            var cipherText = CryptoUtils.Encrypt(messageText, sharedKeys[contactLogin], nonce);
            var message = $"MESSAGE|{myLogin}|{Convert.ToBase64String(cipherText)}|{Convert.ToBase64String(nonce)}|{Convert.ToBase64String(new byte[16])}";

            if (await messageHandler.SendMessageAsync(contactIP, message))
            {
                historyManager.SaveMessage(contactLogin, new LocalMessenger.Core.Models.Message
                {
                    Sender = myLogin,
                    Content = messageText,
                    Type = MessageType.Text,
                    Timestamp = DateTime.Now
                });
                UpdateHistoryDisplay(contactLogin);
                Logger.Log($"Sent message to {contactLogin} (IP: {contactIP}): {messageText}");
            }
            else
            {
                mainForm.AddToBuffer(contactIP, message);
                historyManager.SaveMessage(contactLogin, new LocalMessenger.Core.Models.Message
                {
                    Sender = "SYSTEM",
                    Content = $"Message to {contactLogin} buffered due to send failure.",
                    Type = MessageType.Text,
                    Timestamp = DateTime.Now
                });
                UpdateHistoryDisplay(contactLogin);
                Logger.Log($"Message for {contactLogin} (IP: {contactIP}) added to buffer: {messageText}");
            }
        }

        public async Task HandleSendFileAsync(ContactManager contactManager, FileTransfer fileTransfer, MessageHandler messageHandler, HistoryManager historyManager, Dictionary<string, byte[]> sharedKeys, string myLogin)
        {
            if (!CanSendMessage()) return;
            var contactLogin = SelectedContact;
            var contactIP = contactManager.GetContactIP(contactLogin);
            var mainForm = _form.Controls.OfType<MainForm>().First();

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "All Files (*.*)|*.*|Images (*.jpg, *.png, *.gif)|*.jpg;*.png;*.gif";
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                var filePath = openFileDialog.FileName;
                var fileName = Path.GetFileName(filePath);
                var fileSize = new FileInfo(filePath).Length;
                if (fileSize > 2L * 1024 * 1024 * 1024)
                {
                    ShowError("File size exceeds 2 GB limit.");
                    return;
                }

                var cachedFilePath = fileTransfer.GenerateUniqueFilePath(fileName);
                File.Copy(filePath, cachedFilePath);

                if (!sharedKeys.ContainsKey(contactLogin))
                {
                    Logger.Log($"No shared key found for {contactLogin}, attempting key exchange.");
                    var sharedKey = await mainForm.ExchangeKeysWithContactAsync(contactIP);
                    if (sharedKey == null)
                    {
                        Logger.Log($"Failed to establish connection with {contactLogin} (IP: {contactIP})");
                        ShowError($"Failed to establish connection with {contactLogin}");
                        return;
                    }
                }

                var isImage = new[] { ".jpg", ".png", ".gif" }.Contains(Path.GetExtension(fileName).ToLower());
                var messageType = isImage ? MessageType.Image : MessageType.File;
                var messagePrefix = isImage ? "IMAGE" : "FILE";

                try
                {
                    if (fileSize <= 100 * 1024 * 1024)
                    {
                        await fileTransfer.SendFileAsync(cachedFilePath, contactIP);
                    }
                    else
                    {
                        var sent = await fileTransfer.SendLargeFileAsync(contactIP, messagePrefix, fileName, fileSize, cachedFilePath, sharedKeys[contactLogin], myLogin);
                        if (!sent)
                        {
                            throw new Exception("Large file transfer failed.");
                        }
                    }

                    historyManager.SaveMessage(contactLogin, new LocalMessenger.Core.Models.Message
                    {
                        Sender = myLogin,
                        Content = cachedFilePath,
                        Type = messageType,
                        Timestamp = DateTime.Now
                    });
                    UpdateHistoryDisplay(contactLogin);
                    Logger.Log($"{messageType} sent to {contactLogin} (IP: {contactIP}): {fileName}");
                }
                catch (Exception ex)
                {
                    mainForm.AddToBuffer(contactIP, $"{messagePrefix}|{myLogin}|{fileName}|{fileSize}");
                    historyManager.SaveMessage(contactLogin, new LocalMessenger.Core.Models.Message
                    {
                        Sender = "SYSTEM",
                        Content = $"[SYSTEM] {messageType} to {contactLogin} buffered due to send failure.",
                        Type = MessageType.Text,
                        Timestamp = DateTime.Now
                    });
                    UpdateHistoryDisplay(contactLogin);
                    Logger.Log($"{messageType} for {contactLogin} (IP: {contactIP}) added to buffer due to error: {ex.Message}");
                }
            }
        }

        public void ShowSettingsForm(ref string myLogin, ref string myName, ref string myIP, ref string myStatus, Action reinitializeNetwork)
        {
            var btnOpenSettings = _form.Controls.OfType<Button>().FirstOrDefault(b => b.Name == "btnOpenSettings");
            if (btnOpenSettings == null) return;
            btnOpenSettings.Enabled = false;
            _progressBar.Visible = true;
            _progressBar.Style = ProgressBarStyle.Marquee;
            try
            {
                using (var form = new SettingsForm(myLogin, myName, Configuration.AppDataPath))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        if (myLogin != form.NewLogin || myName != form.NewName)
                        {
                            myLogin = form.NewLogin;
                            myName = form.NewName;
                            _form.Controls.OfType<ComboBox>().FirstOrDefault(c => c.Name == "cmbStatus").SelectedItem = myStatus;
                            UpdateStatusAndIP(myIP, myName, myLogin, myStatus);
                            _contactManager.UpdateContactList();
                            Logger.Log($"User settings updated: Login={myLogin}, Name={myName}");
                            _form.Controls.OfType<MainForm>().First().SaveSettingsPublic();
                        }
                        if (form.SelectedIP != null && form.SelectedIP != myIP)
                        {
                            myIP = form.SelectedIP;
                            reinitializeNetwork();
                            Logger.Log($"Network reinitialized with IP: {myIP}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in settings form: {ex.Message}");
                MessageBox.Show($"Error in settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnOpenSettings.Enabled = true;
                _progressBar.Visible = false;
            }
        }

        public void StopContactUpdateTimer()
        {
            _contactUpdateTimer?.Stop();
        }

        public void OpenSettingsFolder()
        {
            try
            {
                Logger.Log("Opening settings folder");
                Process.Start("explorer.exe", Configuration.AppDataPath);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error opening settings folder: {ex.Message}");
                MessageBox.Show($"Error opening settings folder: {ex.Message}");
            }
        }

        public void DeleteAccount()
        {
            Logger.Log("Delete account initiated");
            var result = MessageBox.Show("Are you sure you want to delete your account? This will remove all user settings.",
                "Confirm Deletion", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.OK)
            {
                try
                {
                    if (File.Exists(Configuration.SettingsFile))
                    {
                        File.Delete(Configuration.SettingsFile);
                        Logger.Log("Account settings deleted successfully");
                    }
                    MessageBox.Show("Account deleted successfully");
                    Application.Exit();
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error deleting account: {ex.Message}");
                    MessageBox.Show($"Error deleting account: {ex.Message}");
                }
            }
            else
            {
                Logger.Log("Account deletion cancelled");
            }
        }

        public void ViewLogs()
        {
            try
            {
                var logDir = Path.Combine(Configuration.AppDataPath, "logs");
                Directory.CreateDirectory(logDir);
                var logFile = Path.Combine(logDir, "log.txt");
                if (!File.Exists(logFile))
                {
                    Logger.Log($"Log file does not exist: {logFile}");
                    MessageBox.Show("Log file does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                Process.Start("notepad.exe", logFile);
                Logger.Log("Opened log file successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error opening log file: {ex.Message}");
                MessageBox.Show($"Failed to open log file: {ex.Message}");
            }
        }

        public void CmbStatus_SelectedIndexChanged(object sender, EventArgs e, ref string myStatus, Action sendStatusUpdate)
        {
            myStatus = _form.Controls.OfType<ComboBox>().FirstOrDefault(c => c.Name == "cmbStatus")?.SelectedItem?.ToString() ?? "Online";
            _contactManager.UpdateStatus(myStatus);
            UpdateStatusAndIP(_form.Controls.OfType<MainForm>().First().GetMyIP(), _form.Controls.OfType<MainForm>().First().GetMyName(), _myLogin, myStatus);
            _form.Controls.OfType<MainForm>().First().SaveSettingsPublic();
            sendStatusUpdate();
            Logger.Log($"Status changed to {myStatus}");
        }

        public void ExitApplication(MessageBufferManager bufferManager)
        {
            Logger.Log("Exit button clicked");
            _form.Controls.OfType<MainForm>().First().SaveSettingsPublic();
            bufferManager.SaveBuffer();
            Application.Exit();
        }
    }
}