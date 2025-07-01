using LocalMessenger.Core.Models;
using LocalMessenger.Core.Security;
using LocalMessenger.Core.Services;
using LocalMessenger.Network.Tcp;
using LocalMessenger.Network.Udp;
using LocalMessenger.UI.Components;
using LocalMessenger.Utilities;
using Message = LocalMessenger.Core.Models.Message;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LocalMessenger.UI.Forms
{
    public partial class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

        private readonly HistoryManager _historyManager;
        private readonly MessageBufferManager _bufferManager;
        private readonly FileTransfer _fileTransfer;
        private UdpManager _udpManager;
        private TcpServer _tcpServer;
        private ECDiffieHellmanCng _myECDH;
        private readonly Dictionary<string, byte[]> _contactPublicKeys;
        private readonly Dictionary<string, byte[]> _sharedKeys;
        private readonly Dictionary<string, string> _contactIPs;
        private readonly Dictionary<string, DateTime> _lastHelloTimes;
        private readonly HashSet<string> _blinkingContacts;
        private readonly ImageList _statusIcons;
        private readonly Icon _appIcon;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private string _myLogin { get; set; }
        private string _myName { get; set; }
        private string _myStatus = "Online";
        private string _myIP;
        private byte[] _encryptionKey;

        public MainForm()
        {
            InitializeComponent();
            _appIcon = Properties.Resources.LocalMessenger;
            this.Icon = _appIcon;

            _myIP = GetLocalIPAddress();
            _contactPublicKeys = new Dictionary<string, byte[]>();
            _sharedKeys = new Dictionary<string, byte[]>();
            _contactIPs = new Dictionary<string, string>();
            _lastHelloTimes = new Dictionary<string, DateTime>();
            _blinkingContacts = new HashSet<string>();
            _cancellationTokenSource = new CancellationTokenSource();
            _statusIcons = new ImageList { ImageSize = new Size(16, 16) };
            _statusIcons.Images.Add("Online", Properties.Resources.Online);
            _statusIcons.Images.Add("Offline", Properties.Resources.Offline);

            InitializeECDH();
            LoadSettings();
            _historyManager = new HistoryManager(Configuration.HistoryPath, _encryptionKey);
            _bufferManager = new MessageBufferManager(Configuration.AppDataPath);
            _fileTransfer = new FileTransfer(_encryptionKey);
            _udpManager = new UdpManager(_myIP, _myLogin, _myName, _myStatus, GetMyPublicKey, HandleUdpMessage);
            _tcpServer = new TcpServer(HandleKeyExchange, HandleMessage, HandleFile);

            new TrayIconManager(this, _appIcon);
            InitializeEmojiMenu();
            ConfigureControls();
            AddCurrentUserToContacts();
            LoadAllHistories();
            UpdateStatusAndIP();
            TrySendBufferedMessagesAsync();

            Task.Run(() => _udpManager.StartBroadcastAsync());
            Task.Run(() => _udpManager.StartListenerAsync());
            Task.Run(() => _tcpServer.StartAsync());
        }

        //private void InitializeBlinkTimer()
        //{
        //    blinkTimer = new Timer { Interval = 2000 };
        //    blinkTimer.Tick += BlinkTimer_Tick;
        //    blinkTimer.Start();
        //}

        private void BlinkTimer_Tick(object sender, EventArgs e)
        {
            lstContacts.Invalidate();
        }

        //private void InitializeStatusIcons()
        //{
        //    statusIcons = new ImageList { ImageSize = new Size(16, 16) };
        //    statusIcons.Images.Add("Online", CreateCircle(Color.Green));
        //    statusIcons.Images.Add("Offline", CreateCircle(Color.Gray));
        //    lstContacts.SmallImageList = statusIcons;
        //}

        private Bitmap CreateCircle(Color color)
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                using (var brush = new SolidBrush(color))
                {
                    g.FillEllipse(brush, 0, 0, 15, 15);
                }
            }
            return bitmap;
        }

        private void InitializeEmojiMenu()
        {
            var emojiMenu = new ContextMenuStrip();
            var emojis = new[] { "😊", "😂", "😍", "😘", "🥰", "🐱", "👍", "🤝", 
            "😢", "🥂", "🤗", "😳", "🤦🏻‍", "🎂", "🍻", "🍾", "🍽", "🚀", "🌟", "❤️"};
            foreach (var emoji in emojis)
            {
                emojiMenu.Items.Add(emoji, null, (s, e) => txtMessage.AppendText(emoji));
            }
            txtMessage.ContextMenuStrip = emojiMenu;
            Logger.Log("Emoji menu initialized with extended emoji set");
        }

        private void ConfigureControls()
        {
            rtbHistory.WordWrap = true;
            rtbHistory.ScrollBars = RichTextBoxScrollBars.Vertical;
            rtbHistory.Font = new Font("Segoe UI Emoji", 10, FontStyle.Regular, GraphicsUnit.Point);
            txtMessage.Font = new Font("Segoe UI Emoji", 10, FontStyle.Regular, GraphicsUnit.Point);
            lstContacts.OwnerDraw = true;
            lstContacts.DrawItem += lstContacts_DrawItem;
            rtbHistory.LinkClicked += rtbHistory_LinkClicked;
            Logger.Log("Controls configured with Segoe UI Emoji font for emoji support");
        }



        private void rtbHistory_LinkClicked(object sender, LinkClickedEventArgs e)
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

        private void txtMessage_ClickDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                e.SuppressKeyPress = true;
                btnSend_Click(sender, e);
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(Configuration.SettingsFile))
                {
                    var json = File.ReadAllText(Configuration.SettingsFile);
                    var settings = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    // Использование TryGetValue вместо GetValueOrDefault
                    settings.TryGetValue("Login", out var login);
                    _myLogin = login ?? Environment.UserName;
                    settings.TryGetValue("Name", out var name);
                    _myName = name ?? Environment.UserName;
                    settings.TryGetValue("Status", out var status);
                    _myStatus = status ?? "Online";
                    settings.TryGetValue("EncryptionKey", out var keyBase64);
                    if (!string.IsNullOrEmpty(keyBase64))
                    {
                        _encryptionKey = Convert.FromBase64String(keyBase64);
                        Logger.Log("Loaded encryption key from settings");
                    }
                    else
                    {
                        _encryptionKey = CryptoUtils.GenerateEncryptionKey();
                        Logger.Log("Generated new encryption key as none was found in settings");
                    }
                    cmbStatus.SelectedItem = _myStatus;
                    Logger.Log($"Settings loaded: Login={_myLogin}, Name={_myName}, Status={_myStatus}");
                }
                else
                {
                    _myLogin = Environment.UserName;
                    _myName = Environment.UserName;
                    _myStatus = "Online";
                    _encryptionKey = CryptoUtils.GenerateEncryptionKey();
                    Logger.Log("No settings file found, initialized with defaults and new encryption key");
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading settings: {ex.Message}");
            }
        }

        //private void LoadSettings()
        //{
        //    if (File.Exists(SettingsFile))
        //    {
        //        try
        //        {
        //            var json = File.ReadAllText(SettingsFile);
        //            if (string.IsNullOrWhiteSpace(json))
        //            {
        //                Logger.Log("Settings file is empty. Showing registration form.");
        //                ShowRegistrationForm();
        //                return;
        //            }
        //            var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        //            if (settings.ContainsKey("login") && settings.ContainsKey("name"))
        //            {
        //                myLogin = settings["login"];
        //                myName = settings["name"];
        //                myStatus = settings.ContainsKey("status") ? settings["status"] : "Online";
        //                cmbStatus.SelectedItem = myStatus;
        //                UpdateStatusAndIP();
        //                Logger.Log($"Settings loaded: Login={myLogin}, Name={myName}, Status={myStatus}");
        //            }
        //            else
        //            {
        //                Logger.Log("Settings file is invalid. Showing registration form.");
        //                ShowRegistrationForm();
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Log($"Error loading settings: {ex.Message}");
        //            MessageBox.Show($"Error loading settings: {ex.Message}");
        //            ShowRegistrationForm();
        //        }
        //    }
        //    else
        //    {
        //        Logger.Log("Settings file not found. Showing registration form.");
        //        ShowRegistrationForm();
        //    }
        //}

        //private void SaveSettings()
        //{
        //    try
        //    {
        //        var settings = new Dictionary<string, string>
        //        {
        //            ["Login"] = _myLogin,
        //            ["Name"] = _myName,
        //            ["Status"] = _myStatus,
        //            ["EncryptionKey"] = Convert.ToBase64String(_encryptionKey)
        //        };
        //        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        //        File.WriteAllText(Configuration.SettingsFile, json);
        //        Logger.Log("Settings saved successfully");
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log($"Error saving settings: {ex.Message}");
        //    }
        //}

        private void SaveSettings()
        {
            try
            {
                var settings = new Dictionary<string, string>
                {
                    ["Login"] = _myLogin,
                    ["Name"] = _myName,
                    ["Status"] = _myStatus,
                    ["EncryptionKey"] = Convert.ToBase64String(_encryptionKey)
                };
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(Configuration.SettingsFile, json);
                Logger.Log("Settings saved successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error saving settings: {ex.Message}");
            }
        }

        //Временно отключаю, т.к. для первого входа будем брать инфу юзера Windows
        //private void ShowRegistrationForm()
        //{
        //    using (var form = new RegistrationForm())
        //    {
        //        if (form.ShowDialog() == DialogResult.OK)
        //        {
        //            myLogin = form.Login;
        //            myName = form.Name;
        //            myStatus = "Online";
        //            SaveSettings();
        //            UpdateStatusAndIP();
        //            AddCurrentUserToContacts();
        //            Logger.Log($"User registered: Login={myLogin}, Name={myName}");
        //        }
        //        else
        //        {
        //            Logger.Log("Registration cancelled. Exiting application.");
        //            Application.Exit();
        //        }
        //    }
        //}


        //   Будет использоваться отдельный метод из класса, на время тестирования отключаю
        //private void InitializeNetwork()
        //{
        //    try
        //    {
        //        udpListener = new UdpClient(new IPEndPoint(IPAddress.Any, 11000));
        //        Logger.Log($"UDP listener initialized and bound to 0.0.0.0:11000");
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log($"Error initializing UDP listener: {ex.Message}");
        //        throw;
        //    }

        //    try
        //    {
        //        udpSender = new UdpClient();
        //        udpSender.Client.Bind(new IPEndPoint(IPAddress.Parse(myIP), 0));
        //        udpSender.EnableBroadcast = true;
        //        Logger.Log($"UDP sender initialized and bound to {myIP}:0 with broadcast enabled");
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log($"Error initializing UDP sender: {ex.Message}");
        //        throw;
        //    }

        //    try
        //    {
        //        tcpListener = new TcpListener(IPAddress.Any, 12000);
        //        Logger.Log("TCP listener initialized for port 12000");
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log($"Error initializing TCP listener: {ex.Message}");
        //        throw;
        //    }

        //    LogNetworkInterfaces();
        //    InitializeECDH();
        //}

        //private void LogNetworkInterfaces()
        //{
        //    var interfaces = NetworkInterface.GetAllNetworkInterfaces()
        //        .Where(n => n.OperationalStatus == OperationalStatus.Up &&
        //                   n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
        //        .ToList();
        //    Logger.Log("Available network interfaces:");
        //    foreach (var ni in interfaces)
        //    {
        //        var ipProps = ni.GetIPProperties();
        //        var ipAddresses = ipProps.UnicastAddresses
        //            .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)
        //            .Select(ip => ip.Address.ToString());
        //        Logger.Log($"Interface: {ni.Name}, IPs: {string.Join(", ", ipAddresses)}");
        //    }
        //}

        //private void InitializeECDH()
        //{
        //    _myECDH = new ECDiffieHellmanCng
        //    {
        //        KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
        //        HashAlgorithm = CngAlgorithm.Sha256
        //    };
        //    Logger.Log("ECDH initialized with SHA256");
        //}

        private void InitializeECDH()
        {
            try
            {
                _myECDH = new ECDiffieHellmanCng
                {
                    KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
                    HashAlgorithm = CngAlgorithm.Sha256
                };
                Logger.Log("ECDH initialized with SHA256");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error initializing ECDH: {ex.Message}");
                throw;
            }
        }

        private string GetLocalIPAddress()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(i => i.OperationalStatus == OperationalStatus.Up && i.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(i => i.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.Address.ToString())
                .ToList();
            var selectedIP = interfaces.FirstOrDefault();
            Logger.Log($"Selected IP address: {selectedIP}");
            return selectedIP;
        }

        private void AddCurrentUserToContacts()
        {
            lstContacts.Items.Add(new ListViewItem($"{_myLogin} ({_myName}, {_myStatus})") { ImageKey = _myStatus == "Online" ? "Online" : "Offline" });
            Logger.Log($"Added current user to contacts: {_myLogin}");
        }

        private void HandleUdpMessage(string message, string remoteIP)
        {
            try
            {
                var parts = message.Split('|');
                if (parts.Length == 5 && parts[0] == "HELLO")
                {
                    var sender = parts[1];
                    var name = parts[2];
                    var status = parts[3];
                    var publicKey = Convert.FromBase64String(parts[4]);

                    if (sender != _myLogin)
                    {
                        _contactPublicKeys[sender] = publicKey;
                        _contactIPs[sender] = remoteIP;
                        _lastHelloTimes[sender] = DateTime.Now;
                        var contactString = $"{sender} ({name}, {status})";
                        var existingItem = lstContacts.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Text.StartsWith(sender));
                        if (existingItem != null)
                        {
                            existingItem.Text = contactString;
                            existingItem.ImageKey = status == "Online" ? "Online" : "Offline";
                            Logger.Log($"Updated contact: {sender} (Name: {name}, Status: {status}, IP: {remoteIP})");
                        }
                        else
                        {
                            var newItem = new ListViewItem(contactString) { ImageKey = status == "Online" ? "Online" : "Offline" };
                            lstContacts.Items.Add(newItem);
                            Logger.Log($"Added contact: {sender} (Name: {name}, Status: {status}, IP: {remoteIP})");
                        }
                        lstContacts.Invalidate();
                    }
                    else
                    {
                        Logger.Log($"Ignored own HELLO message from {remoteIP}");
                    }
                }
                else
                {
                    Logger.Log($"Invalid HELLO message format from {remoteIP}: {message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error parsing UDP message from {remoteIP}: {ex.Message}");
            }
        }

        private byte[] GetMyPublicKey()
        {
            return _myECDH.PublicKey.ToByteArray();
        }

        //private void UpdateHistory(string contact, string content, MessageType type, bool isReceived)
        //{
        //    var msg = new Message
        //    {
        //        Sender = isReceived ? contact : myLogin,
        //        Content = content,
        //        Type = type,
        //        Timestamp = DateTime.Now
        //    };
        //    historyManager.AddMessage(contact, msg);

        //    if (lstContacts.SelectedItems.Count > 0 && lstContacts.SelectedItems[0].Text.StartsWith(contact))
        //    {
        //        UpdateHistoryDisplay(contact);
        //    }
        //    Logger.Log($"Updated history for {contact}: {(isReceived ? "Received" : "Sent")} - {content} ({type})");
        //}

        //private void UpdateGroupHistory(string groupID, string message, bool isReceived)
        //{
        //    Logger.Log($"Updated group history for {groupID}: {(isReceived ? "Received" : "Sent")} - {message}");
        //}


        private void UpdateHistoryDisplay(string contact)
        {
            rtbHistory.Clear();
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
                        rtbHistory.AppendText(prefix + msg.Content + Environment.NewLine);
                        break;
                    case MessageType.File:
                        rtbHistory.AppendText(prefix + $"[File] {Path.GetFileName(msg.Content)}" + Environment.NewLine);
                        rtbHistory.SelectionStart = rtbHistory.TextLength - Path.GetFileName(msg.Content).Length - 1;
                        rtbHistory.SelectionLength = Path.GetFileName(msg.Content).Length;
                        rtbHistory.SelectionColor = Color.Blue;
                        rtbHistory.SelectionFont = new Font(rtbHistory.Font, FontStyle.Underline);
                        break;
                    case MessageType.Image:
                        rtbHistory.AppendText(prefix + $"[Image] {Path.GetFileName(msg.Content)}" + Environment.NewLine);
                        rtbHistory.SelectionStart = rtbHistory.TextLength - Path.GetFileName(msg.Content).Length - 1;
                        rtbHistory.SelectionLength = Path.GetFileName(msg.Content).Length;
                        rtbHistory.SelectionColor = Color.Blue;
                        rtbHistory.SelectionFont = new Font(rtbHistory.Font, FontStyle.Underline);
                        break;
                }
            }
            rtbHistory.ScrollToCaret();
            rtbHistory.SelectAll();
            rtbHistory.SelectionFont = new Font("Segoe UI Emoji", 10, FontStyle.Regular, GraphicsUnit.Point);
            rtbHistory.DeselectAll();
            Logger.Log($"Displayed history for {contact}, {messages.Count} messages loaded");
        }

        private void LoadAllHistories()
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
                if (contact != _myLogin && !lstContacts.Items.Cast<ListViewItem>().Any(i => i.Text.StartsWith(contact)))
                {
                    lstContacts.Items.Add(new ListViewItem($"{contact} ({contact}, Offline)"));
                    Logger.Log($"Added contact with history to lstContacts: {contact} (Offline)");
                }
            }

            if (lstContacts.Items.Count > 0)
            {
                lstContacts.Items[0].Selected = true;
                lstContacts.Select();
                Logger.Log("First contact selected automatically to display history");
            }

            Logger.Log($"Loaded {contactsWithHistory.Count} histories");
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            if (lstContacts.SelectedItems.Count == 0 || string.IsNullOrWhiteSpace(txtMessage.Text)) return;
            var selectedContact = lstContacts.SelectedItems[0].Text;
            var contactLogin = selectedContact.Split(' ')[0];
            if (contactLogin == _myLogin)
            {
                Logger.Log("Attempted to send message to self. Ignored.");
                MessageBox.Show("Cannot send message to yourself.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var messageText = txtMessage.Text;
            txtMessage.Clear();
            byte[] sharedKey = null;
            string contactIP = _contactIPs.ContainsKey(contactLogin) ? _contactIPs[contactLogin] : contactLogin;

            if (!_sharedKeys.ContainsKey(contactLogin))
            {
                Logger.Log($"No shared key found for {contactLogin}, attempting key exchange.");
                sharedKey = await ExchangeKeysWithContactAsync(contactIP);
                if (sharedKey == null)
                {
                    Logger.Log($"Failed to establish connection with {contactLogin} (IP: {contactIP})");
                    MessageBox.Show($"Failed to establish connection with {contactLogin}");
                    return;
                }
            }
            else
            {
                sharedKey = _sharedKeys[contactLogin];
            }

            var nonce = CryptoUtils.GenerateNonce();
            var cipherText = CryptoUtils.Encrypt(messageText, sharedKey, nonce);
            var message = $"MESSAGE|{_myLogin}|{Convert.ToBase64String(cipherText)}|{Convert.ToBase64String(nonce)}|{Convert.ToBase64String(new byte[16])}";

            try
            {
                await SendMessageAsync(contactIP, message);
                _historyManager.SaveMessage(contactLogin, new Message
                {
                    Sender = _myLogin,
                    Content = messageText,
                    Type = MessageType.Text,
                    Timestamp = DateTime.Now
                });
                UpdateHistoryDisplay(contactLogin);
                Logger.Log($"Sent message to {contactLogin} (IP: {contactIP}): {messageText}");
            }
            catch
            {
                _bufferManager.AddToBuffer(contactIP, message);
                _historyManager.SaveMessage(contactLogin, new Message
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


        private async void btnSendFile_Click(object sender, EventArgs e)
        {
            if (lstContacts.SelectedItems.Count == 0) return;
            var selectedContact = lstContacts.SelectedItems[0].Text;
            var contactLogin = selectedContact.Split(' ')[0];
            if (contactLogin == _myLogin)
            {
                Logger.Log("Attempted to send file to self. Ignored.");
                MessageBox.Show("Cannot send file to yourself.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "All Files (*.*)|*.*|Images (*.jpg, *.png, *.gif)|*.jpg;*.png;*.gif";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var filePath = openFileDialog.FileName;
                    var fileName = Path.GetFileName(filePath);
                    var fileSize = new FileInfo(filePath).Length;
                    if (fileSize > 2L * 1024 * 1024 * 1024) // 2 GB limit
                    {
                        MessageBox.Show("File size exceeds 2 GB limit.", "Error");
                        return;
                    }

                    var cachedFilePath = _fileTransfer.GenerateUniqueFilePath(fileName);
                    File.Copy(filePath, cachedFilePath);

                    byte[] sharedKey = null;
                    string contactIP = _contactIPs.ContainsKey(contactLogin) ? _contactIPs[contactLogin] : contactLogin;
                    if (!_sharedKeys.ContainsKey(contactLogin))
                    {
                        Logger.Log($"No shared key found for {contactLogin}, attempting key exchange.");
                        sharedKey = await ExchangeKeysWithContactAsync(contactIP);
                        if (sharedKey == null)
                        {
                            Logger.Log($"Failed to establish connection with {contactLogin} (IP: {contactIP})");
                            MessageBox.Show($"Failed to establish connection with {contactLogin}");
                            return;
                        }
                    }
                    else
                    {
                        sharedKey = _sharedKeys[contactLogin];
                    }

                    var isImage = new[] { ".jpg", ".png", ".gif" }.Contains(Path.GetExtension(fileName).ToLower());
                    var messageType = isImage ? MessageType.Image : MessageType.File;
                    var messagePrefix = isImage ? "IMAGE" : "FILE";
                    bool sent;

                    if (fileSize <= 100 * 1024 * 1024) // 100 MB
                    {
                        var message = $"{messagePrefix}|{_myLogin}|{fileName}|{fileSize}";
                        sent = await _fileTransfer.SendFileAsync(contactIP, message, cachedFilePath);
                    }
                    else
                    {
                        sent = await _fileTransfer.SendLargeFileAsync(contactIP, messagePrefix, fileName, fileSize, cachedFilePath, sharedKey, _myLogin);
                    }

                    if (sent)
                    {
                        _historyManager.SaveMessage(contactLogin, new Message
                        {
                            Sender = _myLogin,
                            Content = cachedFilePath,
                            Type = messageType,
                            Timestamp = DateTime.Now
                        });
                        UpdateHistoryDisplay(contactLogin);
                        Logger.Log($"{messageType} sent to {contactLogin} (IP: {contactIP}): {fileName}");
                    }
                    else
                    {
                        _bufferManager.AddToBuffer(contactIP, $"{messagePrefix}|{_myLogin}|{fileName}|{fileSize}");
                        _historyManager.SaveMessage(contactLogin, new Message
                        {
                            Sender = "SYSTEM",
                            Content = $"[SYSTEM] {messageType} to {contactLogin} buffered due to send failure.",
                            Type = MessageType.Text,
                            Timestamp = DateTime.Now
                        });
                        UpdateHistoryDisplay(contactLogin);
                        Logger.Log($"{messageType} for {contactLogin} (IP: {contactIP}) added to buffer: {fileName}");
                    }
                }
            }
        }


        private async Task<byte[]> ExchangeKeysWithContactAsync(string contactIP)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(contactIP, 12000);
                    var publicKey = GetMyPublicKey();
                    var message = $"KEY_EXCHANGE|{_myLogin}|{Convert.ToBase64String(publicKey)}";
                    var bytes = Encoding.UTF8.GetBytes(message);
                    var stream = client.GetStream();
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                    Logger.Log($"Sent KEY_EXCHANGE to {contactIP}: {message}");
                    return _sharedKeys.ContainsKey(contactIP) ? _sharedKeys[contactIP] : null;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Key exchange failed with {contactIP}: {ex.Message}");
                return null;
            }
        }

        private async Task<bool> SendTcpMessageAsync(string contactIP, string message)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    Logger.Log($"Sending TCP message to {contactIP}: {message}");
                    await client.ConnectAsync(contactIP, 12000);
                    using (var stream = client.GetStream())
                    {
                        var bytes = Encoding.UTF8.GetBytes(message);
                        await stream.WriteAsync(bytes, 0, bytes.Length);
                    }
                    Logger.Log($"Message sent successfully to {contactIP}");
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Message send error to {contactIP}: {ex.Message}");
                    return false;
                }
            }
        }




        //private byte[] EncryptChunk(byte[] data, int length, byte[] key, byte[] nonce)
        //{
        //    using (var aes = Aes.Create())
        //    {
        //        aes.Key = key;
        //        aes.IV = nonce;
        //        aes.Mode = CipherMode.CBC;
        //        aes.Padding = PaddingMode.PKCS7;

        //        using (var ms = new MemoryStream())
        //        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        //        {
        //            cs.Write(data, 0, length);
        //            cs.FlushFinalBlock();
        //            return ms.ToArray();
        //        }
        //    }
        //}

        //private async Task ReceiveLargeFileAsync(NetworkStream stream, string filePath, long fileSize, int chunkSize, byte[] sharedKey)
        //{
        //    using (var fs = File.Create(filePath))
        //    {
        //        var progressForm = new ProgressForm(Path.GetFileName(filePath), fileSize);
        //        progressForm.Show();
        //        long totalRead = 0;

        //        while (totalRead < fileSize)
        //        {
        //            var buffer = new byte[4096];
        //            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        //            var chunkMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        //            var chunkParts = chunkMessage.Split('|');
        //            var encryptedChunk = Convert.FromBase64String(chunkParts[0]);
        //            var nonce = Convert.FromBase64String(chunkParts[1]);
        //            var chunkLength = int.Parse(chunkParts[2]);

        //            var decryptedChunk = DecryptChunk(encryptedChunk, sharedKey, nonce);
        //            await fs.WriteAsync(decryptedChunk, 0, chunkLength);
        //            totalRead += chunkLength;

        //            progressForm.UpdateProgress(totalRead);
        //        }

        //        progressForm.Close();
        //    }

        //    Logger.Log($"Large file received successfully: {filePath}");
        //}

        //private byte[] DecryptChunk(byte[] cipherText, byte[] key, byte[] nonce)
        //{
        //    using (var aes = Aes.Create())
        //    {
        //        aes.Key = key;
        //        aes.IV = nonce;
        //        aes.Mode = CipherMode.CBC;
        //        aes.Padding = PaddingMode.PKCS7;

        //        using (var ms = new MemoryStream())
        //        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
        //        {
        //            cs.Write(cipherText, 0, cipherText.Length);
        //            cs.Flush();
        //            return ms.ToArray();
        //        }
        //    }
        //}

        private async void TrySendBufferedMessagesAsync()
        {
            var messages = _bufferManager.GetBuffer();
            Logger.Log($"Attempting to send {messages.Count} buffered messages");
            foreach (var msg in messages)
            {
                try
                {
                    var parts = msg.Message.Split('|');
                    if (parts[0] == "MESSAGE")
                    {
                        var contact = parts[1];
                        var encryptedMessage = Convert.FromBase64String(parts[2]);
                        var nonce = Convert.FromBase64String(parts[3]);
                        var tag = Convert.FromBase64String(parts[4]);
                        var contactIP = _contactIPs.ContainsKey(contact) ? _contactIPs[contact] : contact;
                        await SendMessageAsync(contactIP, msg.Message);
                    }
                    else if (parts[0].StartsWith("FILE") || parts[0].StartsWith("IMAGE"))
                    {
                        var contact = parts[1];
                        var fileName = parts[2];
                        var fileSize = long.Parse(parts[3]);
                        var contactIP = _contactIPs.ContainsKey(contact) ? _contactIPs[contact] : contact;
                        await _fileTransfer.SendFileAsync(contactIP, msg.Message, Path.Combine(Configuration.AttachmentsPath, fileName));
                    }
                    _bufferManager.RemoveFromBuffer(msg);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error sending buffered message to {msg.ContactIP}: {ex.Message}");
                }
            }
        }

        private void UpdateStatusAndIP()
        {
            lblStatus.Text = $"Status: {_myStatus}";
            lblIP.Text = $"IP: {_myIP}";
            lblUserInfo.Text = $"User: {_myName} ({_myLogin})";

            var currentUserItem = lstContacts.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Text.StartsWith(_myLogin));
            if (currentUserItem != null)
            {
                currentUserItem.Text = $"{_myLogin} ({_myName}, {_myStatus})";
                currentUserItem.ImageKey = _myStatus == "Online" ? "Online" : "Offline";
            }
            Logger.Log($"Updated UI: Status={_myStatus}, IP={_myIP}, User={_myLogin} ({_myName})");
        }

        private void UpdateSendControlsState()
        {
            var isEnabled = lstContacts.SelectedItems.Count > 0 && !lstContacts.SelectedItems[0].Text.StartsWith(_myLogin);
            btnSend.Enabled = isEnabled;
            btnSendFile.Enabled = isEnabled;
            txtMessage.Enabled = isEnabled;
            Logger.Log($"Send controls state updated: {isEnabled}, Selected={(lstContacts.SelectedItems.Count > 0 ? lstContacts.SelectedItems[0].Text : "None")}");
        }

        private void cmbStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            _myStatus = cmbStatus.SelectedItem.ToString();
            UpdateStatusAndIP();
            SaveSettings();
            UpdateContactList();
            SendStatusUpdateBroadcast();
            Logger.Log($"Status changed to {_myStatus}");
        }

        private void lstContacts_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSendControlsState();
            if (lstContacts.SelectedItems.Count > 0)
            {
                var contact = lstContacts.SelectedItems[0].Text.Split(' ')[0];
                UpdateHistoryDisplay(contact);
                _blinkingContacts.Remove(contact);
                Logger.Log($"Selected contact: {lstContacts.SelectedItems[0].Text}");
            }
            else
            {
                rtbHistory.Clear();
                Logger.Log("No contact selected, cleared history display");
            }
        }


        private void UpdateContactList()
        {
            for (int i = 0; i < lstContacts.Items.Count; i++)
            {
                var item = lstContacts.Items[i];
                var login = item.Text.Split(' ')[0];
                if (login == _myLogin)
                {
                    item.Text = $"{_myLogin} ({_myName}, {_myStatus})";
                    item.ImageKey = _myStatus == "Online" ? "Online" : "Offline";
                }
            }
            lstContacts.Invalidate();
            Logger.Log("Updated contact list with current user status");
        }


        //private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        //{
        //    _cancellationTokenSource.Cancel();

        //    SaveSettings();
        //    _bufferManager?.SaveBuffer();
        //    if (udpListener != null)
        //    {
        //        udpListener.Close();
        //        udpListener.Dispose();
        //        Logger.Log("UDP listener closed");
        //    }
        //    if (udpSender != null)
        //    {
        //        udpSender.Close();
        //        udpSender.Dispose();
        //        Logger.Log("UDP sender closed");
        //    }
        //    if (tcpListener != null)
        //    {
        //        tcpListener.Stop();
        //        Logger.Log("TCP listener stopped");
        //    }
        //    //blinkTimer?.Stop();
        //    Logger.Log("Application closing");
        //}

        private void DisposeECDH()
        {
            if (_myECDH != null)
            {
                _myECDH.Clear();
                _myECDH.Dispose();
                _myECDH = null;
                Logger.Log("ECDH keys cleared and disposed");
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_udpManager == null || _tcpServer == null)
            {
                Logger.Log("Failed to initialize network components.");
                MessageBox.Show("Failed to initialize network components.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            _cancellationTokenSource.Cancel();
            _udpManager.Dispose();
            _tcpServer.Stop();
            SaveSettings();
            _bufferManager?.SaveBuffer();
            DisposeECDH();
            Logger.Log("Application closing");
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Logger.Log("Exit button clicked");
            SaveSettings();
            _bufferManager.SaveBuffer();
            Application.Exit();
        }

        //private void btnOpenSettingsFolder_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        Logger.Log("Opening settings folder");
        //        Process.Start("explorer.exe", AppDataPath);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log($"Error opening settings folder: {ex.Message}");
        //        MessageBox.Show($"Error opening settings folder: {ex.Message}");
        //    }
        //}

        //private void btnDeleteAccount_Click(object sender, EventArgs e)
        //{
        //    Logger.Log("Delete account initiated");
        //    var result = MessageBox.Show("Are you sure you want to delete your account? This will remove all user settings.",
        //        "Confirm Deletion", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
        //    if (result == DialogResult.OK)
        //    {
        //        try
        //        {
        //            if (File.Exists(SettingsFile))
        //            {
        //                File.Delete(SettingsFile);
        //                Logger.Log("Account settings deleted successfully");
        //            }
        //            Logger.Log("Account deletion confirmed");
        //            MessageBox.Show("Account deleted successfully");
        //            Application.Exit();
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Log($"Error deleting account: {ex.Message}");
        //            MessageBox.Show($"Error deleting account: {ex.Message}");
        //        }
        //    }
        //    else
        //    {
        //        Logger.Log("Account deletion cancelled");
        //    }
        //}

        private void btnOpenSettingsFolder_Click(object sender, EventArgs e)
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

        private void btnDeleteAccount_Click(object sender, EventArgs e)
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
                    Logger.Log("Account deletion confirmed");
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

        //private void btnViewLogs_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        var logFile = Path.Combine(AppDataPath, "logs", "log.txt");
        //        Process.Start("notepad.exe", logFile);
        //        Logger.Log("Opened log file successfully");
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log($"Error opening log file: {ex.Message}");
        //        MessageBox.Show($"Failed to open log file: {ex.Message}");
        //    }
        //}

        private void btnViewLogs_Click(object sender, EventArgs e)
        {
            try
            {
                var logDir = Path.Combine(Configuration.AppDataPath, "logs");
                Directory.CreateDirectory(logDir);
                var logFile = Path.Combine(Configuration.AppDataPath, "logs", "log.txt");
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

        //private void btnSettings_Click(object sender, EventArgs e)
        //{
        //    using (var form = new SettingsForm(myLogin, myName, AppDataPath))
        //    {
        //        if (form.ShowDialog() == DialogResult.OK)
        //        {
        //            if (myLogin != form.NewLogin || myName != form.NewName)
        //            {
        //                myLogin = form.NewLogin;
        //                myName = form.NewName;
        //                SaveSettings();
        //                UpdateStatusAndIP();
        //                UpdateContactList();
        //                Logger.Log($"User settings updated: Login={myLogin}, Name={myName}");
        //            }
        //            if (form.SelectedIP != null && form.SelectedIP != myIP)
        //            {
        //                myIP = form.SelectedIP;
        //                InitializeNetwork();
        //                Logger.Log($"Network reinitialized with IP: {myIP}");
        //            }
        //        }
        //    }
        //}

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (var form = new SettingsForm(_myLogin, _myName, Configuration.AppDataPath)) // Используем _myLogin, _myName
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    if (_myLogin != form.NewLogin || _myName != form.NewName)
                    {
                        _myLogin = form.NewLogin;
                        _myName = form.NewName;
                        SaveSettings();
                        UpdateStatusAndIP();
                        UpdateContactList();
                        Logger.Log($"User settings updated: Login={_myLogin}, Name={_myName}");
                    }
                    if (form.SelectedIP != null && form.SelectedIP != _myIP)
                    {
                        _myIP = form.SelectedIP;
                        // Перезапускаем сетевые компоненты
                        _udpManager.Dispose();
                        _tcpServer.Stop();
                        //_udpManager = new UdpManager(_myIP, _myLogin, _myName, _myStatus, GetMyPublicKey, HandleUdpMessage);
                        //_tcpServer = new TcpServer(HandleKeyExchange, HandleMessage, HandleFile);
                        //Task.Run(() => _udpManager.StartBroadcastAsync(), _cancellationTokenSource.Token);
                        //Task.Run(() => _udpManager.StartListenerAsync(), _cancellationTokenSource.Token);
                        //Task.Run(() => _tcpServer.StartAsync(), _cancellationTokenSource.Token);

                        try
                        {
                            _udpManager = new UdpManager(_myIP, _myLogin, _myName, _myStatus, GetMyPublicKey, HandleUdpMessage);
                            _tcpServer = new TcpServer(HandleKeyExchange, HandleMessage, HandleFile);
                            Task.Run(() => _udpManager.StartBroadcastAsync(), _cancellationTokenSource.Token);
                            Task.Run(() => _udpManager.StartListenerAsync(), _cancellationTokenSource.Token);
                            Task.Run(() => _tcpServer.StartAsync(), _cancellationTokenSource.Token);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"Error reinitializing network: {ex.Message}");
                            MessageBox.Show($"Failed to reinitialize network: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        Logger.Log($"Network reinitialized with IP: {_myIP}");
                    }
                }
            }
        }

        private byte[] GenerateEncryptionKey()
        {
            var key = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(key);
            }
            Logger.Log("Generated encryption key");
            return key;
        }

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

        private byte[] GenerateGroupKey()
        {
            var key = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(key);
            }
            Logger.Log("Generated group key");
            return key;
        }

        private void txtMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13) // Press ENTER
                btnSend_Click(sender, e);
            //MessageBox.Show("ENTER has been pressed!");
            //else if (e.KeyChar == (char)27)
            //    this.Close();
        }


        private void lstContacts_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            if (e.ItemIndex < 0) return;
            var item = lstContacts.Items[e.ItemIndex];
            var text = item.Text;
            var login = text.Split(' ')[0];
            var status = text.Contains("Online") ? "Online" : "Offline";
            var isBlinking = _blinkingContacts.Contains(login) && (DateTime.Now.Second % 2 == 0);
            e.DrawBackground();
            using (var brush = new SolidBrush(isBlinking ? Color.Yellow : Color.Gray))
            {
                e.Graphics.DrawImage(_statusIcons.Images[status], e.Bounds.Left, e.Bounds.Top);
                e.Graphics.DrawString(text, new Font("Segoe UI Emoji", 9), brush, e.Bounds.Left + 20, e.Bounds.Top);
            }
            e.DrawFocusRectangle();
        }

        //private void lstContacts_DrawItem(object sender, DrawItemEventArgs e)
        //{
        //    if (e.Index < 0) return;
        //    var item = lstContacts.Items[e.Index];
        //    var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        //    var isBlinking = _blinkingContacts.Contains(item.Text.Split(' ')[0]);
        //    var backColor = isSelected ? SystemColors.Highlight : (isBlinking ? Color.Yellow : Color.White);
        //    e.Graphics.FillRectangle(new SolidBrush(backColor), e.Bounds);
        //    var icon = _statusIcons.Images[item.ImageKey];
        //    e.Graphics.DrawImage(icon, e.Bounds.Left, e.Bounds.Top);
        //    var textBounds = new Rectangle(e.Bounds.Left + 20, e.Bounds.Top, e.Bounds.Width - 20, e.Bounds.Height);
        //    e.Graphics.DrawString(item.Text, lstContacts.Font, Brushes.Black, textBounds);
        //    e.DrawFocusRectangle();
        //}

        //    private async void SendStatusUpdateBroadcast()
        //{
        //    try
        //    {
        //        var publicKey = GetMyPublicKey();
        //        var data = $"HELLO|{_myLogin}|{_myName}|{_myStatus}|{Convert.ToBase64String(publicKey)}";
        //        var bytes = Encoding.UTF8.GetBytes(data);
        //        await udpSender.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, 11000));
        //        Logger.Log($"Sent immediate HELLO broadcast with updated status: {data}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log($"Error sending status update broadcast: {ex.Message}");
        //    }
        //}

        //private async void SendStatusUpdateBroadcast()
        //{
        //    try
        //    {
        //        var publicKey = GetMyPublicKey();
        //        var data = $"HELLO|{_myLogin}|{_myName}|{_myStatus}|{Convert.ToBase64String(publicKey)}";
        //        var bytes = Encoding.UTF8.GetBytes(data);
        //        await _udpManager.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, 11000)); // Доступ к udpSender через _udpManager
        //        Logger.Log($"Sent immediate HELLO broadcast with updated status: {data}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log($"Error sending status update broadcast: {ex.Message}");
        //    }
        //}

        private async void SendStatusUpdateBroadcast()
        {
            try
            {
                var publicKey = GetMyPublicKey();
                var data = $"HELLO|{_myLogin}|{_myName}|{_myStatus}|{Convert.ToBase64String(publicKey)}";
                await _udpManager.SendBroadcastAsync(data);
                Logger.Log($"Sent immediate HELLO broadcast with updated status: {data}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error sending status update broadcast: {ex.Message}");
            }
        }

        private void HandleKeyExchange(string sender, byte[] contactPublicKey)
        {
            _contactPublicKeys[sender] = contactPublicKey;
            var sharedKey = _myECDH.DeriveKeyMaterial(CngKey.Import(contactPublicKey, CngKeyBlobFormat.EccPublicBlob));
            _sharedKeys[sender] = sharedKey;
            Logger.Log($"Shared key established with {sender}");
        }


        //private void HandleMessage(string sender, byte[] encryptedMessage, byte[] nonce, byte[] tag)
        //{
        //    var decrypted = CryptoUtils.Decrypt(encryptedMessage, _sharedKeys[sender], nonce);
        //    _historyManager.SaveMessage(sender, new Message
        //    {
        //        Sender = sender,
        //        Content = decrypted,
        //        Type = MessageType.Text,
        //        Timestamp = DateTime.Now
        //    });
        //    UpdateHistoryDisplay(sender);
        //    if (lstContacts.SelectedItems.Count == 0 || !lstContacts.SelectedItems[0].Text.StartsWith(sender))
        //    {
        //        _blinkingContacts.Add(sender);
        //        FlashTaskbar();
        //        if (!this.Visible || this.WindowState == FormWindowState.Minimized)
        //        {
        //            notifyIcon.ShowBalloonTip(3000, "New Message", $"New message from {sender}", ToolTipIcon.Info);
        //            Logger.Log($"Showed balloon tip for new message from {sender}");
        //        }
        //    }
        //    Logger.Log($"Received message from {sender}: {decrypted}");
        //}


        //private void HandleFile(string sender, string fileName, long fileSize, string nonceBase64, bool isChunked)
        //{
        //    var messageType = new[] { ".jpg", ".png", ".gif" }.Contains(Path.GetExtension(fileName).ToLower()) ? MessageType.Image : MessageType.File;
        //    var filePath = _fileTransfer.GenerateUniqueFilePath(fileName);
        //    _historyManager.SaveMessage(sender, new Message
        //    {
        //        Sender = sender,
        //        Content = filePath,
        //        Type = messageType,
        //        Timestamp = DateTime.Now
        //    });
        //    UpdateHistoryDisplay(sender);
        //    if (lstContacts.SelectedItems.Count == 0 || !lstContacts.SelectedItems[0].Text.StartsWith(sender))
        //    {
        //        _blinkingContacts.Add(sender);
        //        FlashTaskbar();
        //        if (!this.Visible || this.WindowState == FormWindowState.Minimized)
        //        {
        //            notifyIcon.ShowBalloonTip(3000, "New File", $"New file from {sender}: {fileName}", ToolTipIcon.Info);
        //            Logger.Log($"Showed balloon tip for new file from {sender}: {fileName}");
        //        }
        //    }
        //    Logger.Log($"Received {messageType} from {sender}: {fileName} saved to {filePath}");
        //}

        private void HandleMessage(string sender, byte[] encryptedMessage, byte[] nonce, byte[] tag)
        {
            var decrypted = CryptoUtils.Decrypt(encryptedMessage, _sharedKeys[sender], nonce);
            _historyManager.SaveMessage(sender, new LocalMessenger.Core.Models.Message
            {
                Sender = sender,
                Content = decrypted,
                Type = MessageType.Text,
                Timestamp = DateTime.Now
            });
            UpdateHistoryDisplay(sender);
            if (lstContacts.SelectedItems.Count == 0 || !lstContacts.SelectedItems[0].Text.StartsWith(sender))
            {
                _blinkingContacts.Add(sender);
                FlashTaskbar();
                if (!this.Visible || this.WindowState == FormWindowState.Minimized)
                {
                    notifyIcon.ShowBalloonTip(3000, "New Message", $"New message from {sender}", ToolTipIcon.Info);
                    Logger.Log($"Showed balloon tip for new message from {sender}");
                }
            }
            Logger.Log($"Received message from {sender}: {decrypted}");
        }

        private void HandleFile(string sender, string fileName, long fileSize, string nonceBase64, bool isChunked)
        {
            var messageType = new[] { ".jpg", ".png", ".gif" }.Contains(Path.GetExtension(fileName).ToLower()) ? MessageType.Image : MessageType.File;
            var filePath = _fileTransfer.GenerateUniqueFilePath(fileName);
            _historyManager.SaveMessage(sender, new LocalMessenger.Core.Models.Message
            {
                Sender = sender,
                Content = filePath,
                Type = messageType,
                Timestamp = DateTime.Now
            });
            UpdateHistoryDisplay(sender);
            if (lstContacts.SelectedItems.Count == 0 || !lstContacts.SelectedItems[0].Text.StartsWith(sender))
            {
                _blinkingContacts.Add(sender);
                FlashTaskbar();
                if (!this.Visible || this.WindowState == FormWindowState.Minimized)
                {
                    notifyIcon.ShowBalloonTip(3000, "New File", $"New file from {sender}: {fileName}", ToolTipIcon.Info);
                    Logger.Log($"Showed balloon tip for new file from {sender}: {fileName}");
                }
            }
            Logger.Log($"Received {messageType} from {sender}: {fileName} saved to {filePath}");
        }


        private void FlashTaskbar()
        {
            if (!this.Visible || this.WindowState == FormWindowState.Minimized)
            {
                FlashWindow(this.Handle, true);
                Logger.Log("Taskbar flashed for new message");
            }
        }


        private async Task SendMessageAsync(string contactIP, string message)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(contactIP, 12000);
                    var stream = client.GetStream();
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                    Logger.Log($"Sent message to {contactIP}: {message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error sending message to {contactIP}: {ex.Message}");
                throw;
            }
        }

        


        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _cancellationTokenSource.Cancel();
            Logger.Log("Application closing");
        }

    }
}