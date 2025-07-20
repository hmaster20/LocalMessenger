using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LocalMessenger
{
    public partial class MainForm : Form
    {
        private string AppDataPath;
        private string AttachmentsPath;
        private string HistoryPath;
        private string SettingsFile;

        private UdpClient udpListener;
        private UdpClient udpSender;
        private TcpListener tcpListener;
        private ECDiffieHellmanCng myECDH;
        private Dictionary<string, byte[]> contactPublicKeys = new Dictionary<string, byte[]>();
        private Dictionary<string, byte[]> sharedKeys = new Dictionary<string, byte[]>();
        private Dictionary<string, byte[]> groupKeys = new Dictionary<string, byte[]>();
        private Dictionary<string, string> contactIPs = new Dictionary<string, string>();

        private string myLogin;
        private string myName;
        private string myStatus = "Online";
        private string myIP = GetLocalIPAddress();
        private HistoryManager historyManager;
        private MessageBufferManager bufferManager;
        private byte[] encryptionKey;

        //private Timer blinkTimer;
        private HashSet<string> blinkingContacts = new HashSet<string>();

        private ImageList statusIcons;
        private Icon appIcon; // Поле для хранения иконки

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Dictionary<string, DateTime> lastHelloTimes = new Dictionary<string, DateTime>();

        public MainForm()
        {
            InitializeComponent();
            // Загружаем иконку из ресурсов
            appIcon = Properties.Resources.LocalMessenger; // Убедитесь, что имя ресурса совпадает
            this.Icon = appIcon; // Устанавливаем иконку для формы
            //InitializeBlinkTimer();
            InitializeStatusIcons();
            InitializeEmojiMenu();
            Logger.Log($"Application started. Session initialized for IP: {myIP}");
            InitializePaths();
            InitializeDirectories();
            LoadSettingsAndKey();
            InitializeNetwork();
            historyManager = new HistoryManager(AppDataPath, encryptionKey);
            bufferManager = new MessageBufferManager(AppDataPath);
            new TrayIconManager(this, appIcon); // Передаём иконку в TrayIconManager
            AddCurrentUserToContacts();
            StartUdpBroadcast();
            StartUdpListener();
            StartTcpServer();
            UpdateStatusAndIP();
            TrySendBufferedMessagesAsync();
            UpdateSendControlsState();
            LoadAllHistories();
            ConfigureControls();
        }

        private void LoadSettingsAndKey()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    var json = File.ReadAllText(SettingsFile);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Logger.Log("Settings file is empty. Showing registration form.");
                        ShowRegistrationForm();
                        return;
                    }
                    var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (settings.ContainsKey("login") && settings.ContainsKey("name"))
                    {
                        myLogin = settings["login"];
                        myName = settings["name"];
                        myStatus = settings.ContainsKey("status") ? settings["status"] : "Online";
                        cmbStatus.SelectedItem = myStatus;

                        // Запрашиваем пароль для загрузки ключа
                        using (var passwordForm = new PasswordForm())
                        {
                            if (passwordForm.ShowDialog() == DialogResult.OK)
                            {
                                encryptionKey = KeyManager.LoadKey(passwordForm.Password);
                            }
                            else
                            {
                                Logger.Log("Password entry cancelled. Exiting application.");
                                Application.Exit();
                            }
                        }

                        UpdateStatusAndIP();
                        Logger.Log($"Settings loaded: Login={myLogin}, Name={myName}, Status={myStatus}");
                    }
                    else
                    {
                        Logger.Log("Settings file is invalid. Showing registration form.");
                        ShowRegistrationForm();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error loading settings or key: {ex.Message}");
                    MessageBox.Show($"Error loading settings or key: {ex.Message}");
                    ShowRegistrationForm();
                }
            }
            else
            {
                Logger.Log("Settings file not found. Showing registration form.");
                ShowRegistrationForm();
            }
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

        private void InitializeStatusIcons()
        {
            statusIcons = new ImageList { ImageSize = new Size(16, 16) };
            statusIcons.Images.Add("Online", CreateCircle(Color.Green));
            statusIcons.Images.Add("Offline", CreateCircle(Color.Gray));
            lstContacts.SmallImageList = statusIcons;
        }

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
            var emojis = new[] { "😊", "😂", "👍", "😢", "😍" };
            foreach (var emoji in emojis)
            {
                emojiMenu.Items.Add(emoji, null, (s, e) => txtMessage.AppendText(emoji));
            }
            txtMessage.ContextMenuStrip = emojiMenu;
        }

        private void ConfigureControls()
        {
            //txtMessage.KeyDown += txtMessage_KeyDown;
            rtbHistory.WordWrap = true;
            rtbHistory.ScrollBars = RichTextBoxScrollBars.Vertical;
            rtbHistory.Font = new Font("Segoe UI Emoji", 10);
            txtMessage.Font = new Font("Segoe UI Emoji", 10);
            lstContacts.OwnerDraw = true;
            lstContacts.DrawItem += lstContacts_DrawItem;
            rtbHistory.LinkClicked += rtbHistory_LinkClicked;

            // Добавляем поддержку Drag & Drop
            rtbHistory.AllowDrop = true;
            rtbHistory.DragEnter += rtbHistory_DragEnter;
            rtbHistory.DragDrop += rtbHistory_DragDrop;
        }

        private async Task SendFilesToContactAsync(IEnumerable<string> filePaths)
        {
            if (lstContacts.SelectedItems.Count == 0) return;

            var selectedContact = lstContacts.SelectedItems[0].Text;
            var contactLogin = selectedContact.Split(' ')[0];

            if (contactLogin == myLogin)
            {
                Logger.Log("Attempted to send file to self. Ignored.");
                MessageBox.Show("Cannot send file to yourself.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath)) continue;

                var fileName = Path.GetFileName(filePath);
                var fileSize = new FileInfo(filePath).Length;

                if (fileSize > 2L * 1024 * 1024 * 1024) // 2 GB limit
                {
                    MessageBox.Show($"File size exceeds 2 GB limit: {fileName}", "Error");
                    continue;
                }

                var cachedFilePath = Path.Combine(AttachmentsPath, $"{Guid.NewGuid()}_{fileName}");
                File.Copy(filePath, cachedFilePath);

                byte[] sharedKey = null;
                string contactIP = contactIPs.ContainsKey(contactLogin) ? contactIPs[contactLogin] : contactLogin;
                if (!sharedKeys.ContainsKey(contactLogin))
                {
                    Logger.Log($"No shared key found for {contactLogin}, attempting key exchange.");
                    sharedKey = await ExchangeKeysWithContactAsync(contactIP);
                    if (sharedKey == null)
                    {
                        Logger.Log($"Failed to establish connection with {contactLogin} (IP: {contactIP})");
                        MessageBox.Show($"Failed to establish connection with {contactLogin}");
                        continue;
                    }
                }
                else
                {
                    sharedKey = sharedKeys[contactLogin];
                }

                var isImage = new[] { ".jpg", ".png", ".gif" }.Contains(Path.GetExtension(fileName).ToLower());
                var messageType = isImage ? MessageType.Image : MessageType.File;
                var messagePrefix = isImage ? "IMAGE" : "FILE";
                bool sent;

                if (fileSize <= 100 * 1024 * 1024) // 100 MB
                {
                    var message = $"{messagePrefix}|{myLogin}|{fileName}|{fileSize}";
                    sent = await SendFileAsync(contactIP, message, cachedFilePath);
                }
                else
                {
                    sent = await SendLargeFileAsync(contactIP, messagePrefix, fileName, fileSize, cachedFilePath, sharedKey);
                }

                if (sent)
                {
                    UpdateHistory(contactLogin, cachedFilePath, messageType, isReceived: false);
                    Logger.Log($"{messageType} sent to {contactLogin} (IP: {contactIP}): {fileName}");
                }
                else
                {
                    bufferManager.AddToBuffer(contactIP, $"{messagePrefix}|{myLogin}|{fileName}|{fileSize}");
                    UpdateHistory(contactLogin, $"[SYSTEM] {messageType} to {contactLogin} buffered due to send failure.", MessageType.Text, isReceived: false);
                    Logger.Log($"{messageType} for {contactLogin} (IP: {contactIP}) added to buffer: {fileName}");
                }
            }
        }

        private async void btnSendFile_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "All Files (*.*)|*.*|Images (*.jpg, *.png, *.gif)|*.jpg;*.png;*.gif";
                openFileDialog.Multiselect = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    await SendFilesToContactAsync(openFileDialog.FileNames);
                }
            }
        }

        private async void rtbHistory_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                await SendFilesToContactAsync(files);
            }
        }

        private void rtbHistory_DragEnter(object sender, DragEventArgs e)
        {
            // Проверяем, что перетаскиваются файлы
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void rtbHistory_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", e.LinkText);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error opening file: {ex.Message}");
                MessageBox.Show($"Error opening file: {ex.Message}");
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

        private void InitializePaths()
        {
            AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalMessenger");
            AttachmentsPath = Path.Combine(AppDataPath, "attachments");
            HistoryPath = Path.Combine(AppDataPath, "history");
            SettingsFile = Path.Combine(AppDataPath, "settings.json");
            Logger.Log($"Paths initialized: AppData={AppDataPath}, Settings={SettingsFile}");
        }

        private void InitializeDirectories()
        {
            Directory.CreateDirectory(AppDataPath);
            Directory.CreateDirectory(AttachmentsPath);
            Directory.CreateDirectory(HistoryPath);
            Logger.Log("Directories created or verified");
        }

        private void ShowRegistrationForm()
        {
            using (var form = new RegistrationForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    myLogin = form.Login;
                    myName = form.Name;
                    myStatus = "Online";
                    encryptionKey = KeyManager.GenerateAndSaveKey(form.Password); // Сохраняем ключ
                    SaveSettings();
                    UpdateStatusAndIP();
                    AddCurrentUserToContacts();
                    Logger.Log($"User registered: Login={myLogin}, Name={myName}");
                }
                else
                {
                    Logger.Log("Registration cancelled. Exiting application.");
                    Application.Exit();
                }
            }
        }

        private void InitializeNetwork()
        {
            try
            {
                udpListener = new UdpClient(new IPEndPoint(IPAddress.Any, 11000));
                Logger.Log($"UDP listener initialized and bound to 0.0.0.0:11000");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error initializing UDP listener: {ex.Message}");
                throw;
            }

            try
            {
                udpSender = new UdpClient();
                udpSender.Client.Bind(new IPEndPoint(IPAddress.Parse(myIP), 0));
                udpSender.EnableBroadcast = true;
                Logger.Log($"UDP sender initialized and bound to {myIP}:0 with broadcast enabled");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error initializing UDP sender: {ex.Message}");
                throw;
            }

            try
            {
                tcpListener = new TcpListener(IPAddress.Any, 12000);
                Logger.Log("TCP listener initialized for port 12000");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error initializing TCP listener: {ex.Message}");
                throw;
            }

            LogNetworkInterfaces();
            InitializeECDH();
        }

        private void LogNetworkInterfaces()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                           n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .ToList();
            Logger.Log("Available network interfaces:");
            foreach (var ni in interfaces)
            {
                var ipProps = ni.GetIPProperties();
                var ipAddresses = ipProps.UnicastAddresses
                    .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(ip => ip.Address.ToString());
                Logger.Log($"Interface: {ni.Name}, IPs: {string.Join(", ", ipAddresses)}");
            }
        }

        private void InitializeECDH()
        {
            myECDH = new ECDiffieHellmanCng
            {
                KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
                HashAlgorithm = CngAlgorithm.Sha256
            };
            Logger.Log("ECDH initialized with SHA256");
        }

        private static string GetLocalIPAddress()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                           n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                           !n.Name.ToLower().Contains("virtualbox") &&
                           !n.Name.ToLower().Contains("vmware") &&
                           !n.Name.ToLower().Contains("virtual"))
                .ToList();

            foreach (var ni in interfaces)
            {
                var ipProps = ni.GetIPProperties();
                if (ipProps.GatewayAddresses.Any(g => g.Address != null))
                {
                    foreach (var ip in ipProps.UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            var ipBytes = ip.Address.GetAddressBytes();
                            if (ipBytes[0] == 192 && ipBytes[1] == 168 && ipBytes[2] != 56)
                            {
                                Logger.Log($"Selected IP address: {ip.Address} (Interface: {ni.Name})");
                                return ip.Address.ToString();
                            }
                        }
                    }
                }
            }

            foreach (var ni in interfaces)
            {
                var ipProps = ni.GetIPProperties();
                foreach (var ip in ipProps.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var ipBytes = ip.Address.GetAddressBytes();
                        if (ipBytes[0] == 192 && ipBytes[1] == 168 && ipBytes[2] != 56)
                        {
                            Logger.Log($"Selected fallback IP address: {ip.Address} (Interface: {ni.Name})");
                            return ip.Address.ToString();
                        }
                    }
                }
            }

            Logger.Log("No suitable network interface found in 192.168.0.0/16 range, excluding virtual networks");
            throw new Exception("No network interface found with an IP address in the 192.168.0.0/16 range, excluding virtual networks!");
        }

        private void AddCurrentUserToContacts()
        {
            Logger.Log($"Adding {myLogin} ({myName}, {myStatus}) to lstContacts");
            if (!lstContacts.Items.Cast<ListViewItem>().Any(i => (string)i.Tag == myLogin))
            {
                var item = new ListViewItem($"{myLogin} ({myName}, {myStatus})") { Tag = myLogin };
                lstContacts.Items.Add(item);
                Logger.Log($"Added current user to contacts: {myLogin}");
            }
        }

        private async void StartUdpBroadcast()
        {
            Logger.Log("Starting UDP broadcast for user discovery", isHeartbeat: true);
            while (true)
            {
                try
                {
                    var publicKey = GetMyPublicKey();
                    var data = $"HELLO|{myLogin}|{myName}|{myStatus}|{Convert.ToBase64String(publicKey)}";
                    var bytes = Encoding.UTF8.GetBytes(data);
                    await udpSender.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, 11000));
                    Logger.Log($"Sent HELLO broadcast from {myIP}: {data}", isHeartbeat: true);
                    await Task.Delay(15000); // HeartBit
                }
                catch (Exception ex)
                {
                    Logger.Log($"Broadcast error: {ex.Message}", isHeartbeat: true);
                    MessageBox.Show($"Broadcast error: {ex.Message}");
                }
            }
        }

        private async void StartUdpListener()
        {
            Logger.Log("Starting UDP listener on port 11000");
            try
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var result = await Task.Run(() => udpListener.ReceiveAsync(), cancellationTokenSource.Token);
                        var message = Encoding.UTF8.GetString(result.Buffer);
                        var remoteIP = result.RemoteEndPoint.Address.ToString();
                        Logger.Log($"Received UDP message from {remoteIP}: {message}");

                        HandleUdpMessage(message, remoteIP);
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Log("UDP listener cancelled");
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        Logger.Log("UDP listener disposed");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"UDP listener error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Unexpected error in UDP listener: {ex.Message}");
            }
        }

        private void HandleUdpMessage(string message, string remoteIP)
        {
            try
            {
                var parts = message.Split('|');
                if (parts.Length == 5 && parts[0] == "HELLO")
                {
                    var sender = parts[1].Trim(); // Удаляем пробелы
                    if (string.IsNullOrWhiteSpace(sender) || sender.Contains(" "))
                    {
                        Logger.Log($"Invalid login format from {remoteIP}: {sender}");
                        return;
                    }
                    var name = parts[2];
                    var status = parts[3];
                    var publicKey = Convert.FromBase64String(parts[4]);

                    if (sender != myLogin)
                    {
                        contactPublicKeys[sender] = publicKey;
                        contactIPs[sender] = remoteIP;
                        lastHelloTimes[sender] = DateTime.Now;
                        var contactString = $"{sender} ({name}, {status})";
                        var existingItem = lstContacts.Items.Cast<ListViewItem>().FirstOrDefault(i => (string)i.Tag == sender);
                        if (existingItem != null)
                        {
                            existingItem.Text = contactString;
                            Logger.Log($"Updated contact: {sender} (Name: {name}, Status: {status}, IP: {remoteIP})");
                        }
                        else
                        {
                            var newItem = new ListViewItem(contactString) { Tag = sender };
                            lstContacts.Items.Add(newItem);
                            Logger.Log($"Added contact: {sender} (Name: {name}, Status: {status}, IP: {remoteIP})");
                        }
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
            return myECDH.PublicKey.ToByteArray();
        }

        private async void StartTcpServer()
        {
            try
            {
                tcpListener.Start();
                Logger.Log("TCP server started on port 12000");
                while (true)
                {
                    var client = await tcpListener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Server error: {ex.Message}");
                MessageBox.Show($"Server error: {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                try
                {
                    var remoteEndPoint = client.Client.RemoteEndPoint.ToString();
                    Logger.Log($"Handling TCP client connection from {remoteEndPoint}");
                    var stream = client.GetStream();
                    var buffer = new byte[4096];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Logger.Log($"Received TCP message: {message}");
                    var parts = message.Split('|');

                    if (parts[0] == "KEY_EXCHANGE")
                    {
                        var sender = parts[1];
                        var contactPublicKey = Convert.FromBase64String(parts[2]);
                        contactPublicKeys[sender] = contactPublicKey;

                        var publicKey = GetMyPublicKey();
                        var response = $"KEY_EXCHANGE_RESPONSE|{myLogin}|{Convert.ToBase64String(publicKey)}";
                        var bytes = Encoding.UTF8.GetBytes(response);
                        await stream.WriteAsync(bytes, 0, bytes.Length);
                        Logger.Log($"Sent KEY_EXCHANGE_RESPONSE to {sender}");

                        var sharedKey = myECDH.DeriveKeyMaterial(CngKey.Import(contactPublicKey, CngKeyBlobFormat.EccPublicBlob));
                        sharedKeys[sender] = sharedKey;
                        Logger.Log($"Shared key established with {myLogin}");
                    }
                    else if (parts[0] == "KEY_EXCHANGE_RESPONSE")
                    {
                        var sender = parts[1];
                        var contactPublicKey = Convert.FromBase64String(parts[2]);
                        var sharedKey = myECDH.DeriveKeyMaterial(CngKey.Import(contactPublicKey, CngKeyBlobFormat.EccPublicBlob));
                        sharedKeys[sender] = sharedKey;
                        Logger.Log($"Shared key for {sender} is set");
                    }
                    else if (parts[0] == "MESSAGE")
                    {
                        var sender = parts[1];
                        var encryptedMessage = Convert.FromBase64String(parts[2]);
                        var nonce = Convert.FromBase64String(parts[3]);
                        var tag = Convert.FromBase64String(parts[4]);

                        var decrypted = Decrypt(encryptedMessage, sharedKeys[sender], nonce, tag);
                        UpdateHistory(sender, decrypted, MessageType.Text, isReceived: true);
                        if (lstContacts.SelectedItems.Count == 0 || !lstContacts.SelectedItems[0].Text.StartsWith(sender))
                        {
                            blinkingContacts.Add(sender);
                        }
                        Logger.Log($"Received MESSAGE from {sender}: {decrypted}");
                    }
                    else if (parts[0].StartsWith("FILE") || parts[0].StartsWith("IMAGE"))
                    {
                        var isChunked = parts[0].EndsWith("_CHUNKED");
                        var sender = parts[1];
                        var fileName = parts[2];
                        var fileSize = long.Parse(parts[3]);
                        var filePath = Path.Combine(AttachmentsPath, $"{Guid.NewGuid()}_{fileName}");
                        var messageType = parts[0].StartsWith("IMAGE") ? MessageType.Image : MessageType.File;

                        if (!isChunked)
                        {
                            using (var fs = File.Create(filePath))
                            {
                                var totalRead = 0L;
                                while (totalRead < fileSize)
                                {
                                    var toRead = (int)Math.Min(buffer.Length, fileSize - totalRead);
                                    bytesRead = await stream.ReadAsync(buffer, 0, toRead);
                                    await fs.WriteAsync(buffer, 0, bytesRead);
                                    totalRead += bytesRead;
                                }
                            }
                        }
                        else
                        {
                            var chunkSize = int.Parse(parts[4]);
                            await ReceiveLargeFileAsync(stream, filePath, fileSize, chunkSize, sharedKeys[sender]);
                        }

                        UpdateHistory(sender, filePath, messageType, isReceived: true);
                        if (lstContacts.SelectedItems.Count == 0 || !lstContacts.SelectedItems[0].Text.StartsWith(sender))
                        {
                            blinkingContacts.Add(sender);
                        }
                        Logger.Log($"Received {messageType} from {sender}: {fileName} saved to {filePath}");
                    }
                    else if (parts[0] == "GROUP_MESSAGE")
                    {
                        var groupID = parts[1];
                        var sender = parts[5];
                        var encryptedMessage = Convert.FromBase64String(parts[2]);
                        var nonce = Convert.FromBase64String(parts[3]);
                        var tag = Convert.FromBase64String(parts[4]);

                        var decrypted = Decrypt(encryptedMessage, groupKeys[groupID], nonce, tag);
                        UpdateGroupHistory(groupID, decrypted, isReceived: true);
                        Logger.Log($"Received GROUP_MESSAGE for {groupID} from {sender}: {decrypted}");
                    }
                    else if (parts[0] == "GROUP_KEY")
                    {
                        var groupID = parts[1];
                        var sender = parts[5];
                        var encryptedGroupKey = Convert.FromBase64String(parts[2]);
                        var nonce = Convert.FromBase64String(parts[3]);
                        var tag = Convert.FromBase64String(parts[4]);

                        if (sharedKeys.ContainsKey(sender))
                        {
                            var decryptedGroupKeyString = Decrypt(encryptedGroupKey, sharedKeys[sender], nonce, tag);
                            groupKeys[groupID] = Convert.FromBase64String(decryptedGroupKeyString);
                            Logger.Log($"Received GROUP_KEY for {groupID} from {sender}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error handling client {client.Client.RemoteEndPoint}: {ex.Message}");
                }
            }
        }

        private byte[] Encrypt(string plainText, byte[] key, byte[] nonce)
        {
            Logger.Log($"Encrypting text: {plainText}");
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = nonce;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                {
                    var plainBytes = Encoding.UTF8.GetBytes(plainText);
                    var result = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    Logger.Log($"Encrypted text length: {result.Length}");
                    return result;
                }
            }
        }

        private string Decrypt(byte[] cipherText, byte[] key, byte[] nonce, byte[] tag)
        {
            Logger.Log($"Decrypting cipher text length: {cipherText.Length}");
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = nonce;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                {
                    var decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                    var result = Encoding.UTF8.GetString(decryptedBytes);
                    Logger.Log($"Decrypted text: {result}");
                    return result;
                }
            }
        }

        private byte[] GenerateNonce()
        {
            var nonce = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(nonce);
            }
            return nonce;
        }

        private void UpdateHistory(string contact, string content, MessageType type, bool isReceived)
        {
            var msg = new Message
            {
                Sender = isReceived ? contact : myLogin,
                Content = content,
                Type = type,
                Timestamp = DateTime.Now
            };
            historyManager.AddMessage(contact, msg);

            if (lstContacts.SelectedItems.Count > 0 && lstContacts.SelectedItems[0].Text.StartsWith(contact))
            {
                UpdateHistoryDisplay(contact);
            }
            Logger.Log($"Updated history for {contact}: {(isReceived ? "Received" : "Sent")} - {content} ({type})");
        }

        private void UpdateGroupHistory(string groupID, string message, bool isReceived)
        {
            Logger.Log($"Updated group history for {groupID}: {(isReceived ? "Received" : "Sent")} - {message}");
        }
        private void UpdateHistoryDisplay(string contact)
        {
            rtbHistory.Clear();
            var messages = historyManager.LoadMessages(contact);
            if (messages.Count == 0)
            {
                Logger.Log($"No messages found for contact: {contact}");
                rtbHistory.AppendText($"Нет сообщений для {contact}" + Environment.NewLine);
            }
            else
            {
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
            }
            rtbHistory.ScrollToCaret();
            Logger.Log($"Displayed history for {contact} (Messages: {messages.Count})");
        }
        private void LoadAllHistories()
        {
            var historyFiles = Directory.GetFiles(HistoryPath, "*.json");
            var contactsWithHistory = new HashSet<string>();

            foreach (var file in historyFiles)
            {
                var contact = Path.GetFileNameWithoutExtension(file);
                contactsWithHistory.Add(contact);
                var messages = historyManager.LoadMessages(contact);
                Logger.Log($"Loaded history for {contact}: {messages.Count} messages");
            }

            foreach (var contact in contactsWithHistory)
            {
                if (contact != myLogin && !lstContacts.Items.Cast<ListViewItem>().Any(i => (string)i.Tag == contact))
                {
                    var item = new ListViewItem($"{contact} ({contact}, Offline)") { Tag = contact };
                    lstContacts.Items.Add(item);
                    Logger.Log($"Added contact with history to lstContacts: {contact} (Offline)");
                }
            }

            Logger.Log($"Loaded {contactsWithHistory.Count} histories");
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            if (lstContacts.SelectedItems.Count == 0) return;
            var selectedContact = lstContacts.SelectedItems[0].Text;
            var contactLogin = selectedContact.Split(' ')[0];
            if (contactLogin == myLogin)
            {
                Logger.Log("Attempted to send message to self. Ignored.");
                MessageBox.Show("Cannot send messages to yourself.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                byte[] sharedKey = null;
                string contactIP = contactIPs.ContainsKey(contactLogin) ? contactIPs[contactLogin] : contactLogin;
                if (!sharedKeys.ContainsKey(contactLogin))
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
                    sharedKey = sharedKeys[contactLogin];
                }

                var nonce = GenerateNonce();
                var encrypted = Encrypt(txtMessage.Text, sharedKey, nonce);

                var message = $"MESSAGE|{myLogin}|{Convert.ToBase64String(encrypted)}|{Convert.ToBase64String(nonce)}|{Convert.ToBase64String(new byte[16])}";
                bool sent = await SendTcpMessageAsync(contactIP, message);

                if (sent)
                {
                    UpdateHistory(contactLogin, txtMessage.Text, MessageType.Text, isReceived: false);
                    txtMessage.Clear();
                    Logger.Log($"Message sent to {contactLogin} (IP: {contactIP}): {txtMessage.Text}");
                }
                else
                {
                    bufferManager.AddToBuffer(contactIP, message);
                    UpdateHistory(contactLogin, $"[SYSTEM] Message to {contactLogin} buffered due to send failure.", MessageType.Text, isReceived: false);
                    Logger.Log($"Message for {contactLogin} (IP: {contactIP}) added to buffer: {txtMessage.Text}");
                }
            }
        }

        private async Task<byte[]> ExchangeKeysWithContactAsync(string contactIP)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    Logger.Log($"Initiating key exchange with {contactIP}");
                    await client.ConnectAsync(contactIP, 12000);
                    var stream = client.GetStream();

                    var publicKey = GetMyPublicKey();
                    var message = $"KEY_EXCHANGE|{myLogin}|{Convert.ToBase64String(publicKey)}";
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                    Logger.Log($"Sent {message} to {myLogin}");

                    var buffer = new byte[4096];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Logger.Log($"Received response: {response}");
                    var parts = response.Split('|');

                    if (parts[0] == "KEY_EXCHANGE_RESPONSE")
                    {
                        var sender = parts[1];
                        var contactPublicKey = Convert.FromBase64String(parts[2]);
                        contactPublicKeys[sender] = contactPublicKey;

                        var sharedKey = myECDH.DeriveKeyMaterial(CngKey.Import(contactPublicKey, CngKeyBlobFormat.EccPublicBlob));
                        sharedKeys[sender] = sharedKey;
                        contactIPs[sender] = contactIP;
                        Logger.Log($"Key exchange completed with {sender} (IP: {contactIP})");
                        return sharedKey;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Key exchange error with {contactIP}: {ex.Message}");
                    MessageBox.Show($"Key exchange error: {ex.Message}");
                }
            }
            return null;
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

        private async Task<bool> SendFileAsync(string contactIP, string message, string filePath)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync(contactIP, 12000);
                    using (var stream = client.GetStream())
                    {
                        var messageBytes = Encoding.UTF8.GetBytes(message);
                        await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                        await stream.FlushAsync();

                        using (var fs = File.OpenRead(filePath))
                        {
                            var buffer = new byte[4096];
                            int bytesRead;
                            while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await stream.WriteAsync(buffer, 0, bytesRead);
                            }
                        }
                    }
                    Logger.Log($"File sent successfully to {contactIP}: {filePath}");
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"File send error to {contactIP}: {ex.Message}");
                    return false;
                }
            }
        }

        private async Task<bool> SendLargeFileAsync(string contactIP, string messagePrefix, string fileName, long fileSize, string filePath, byte[] sharedKey)
        {
            const int chunkSize = 1024 * 1024; // 1 MB chunks
            using (var client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync(contactIP, 12000);
                    using (var stream = client.GetStream())
                    {
                        // Send file metadata
                        var message = $"{messagePrefix}_CHUNKED|{myLogin}|{fileName}|{fileSize}|{chunkSize}";
                        var messageBytes = Encoding.UTF8.GetBytes(message);
                        await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                        await stream.FlushAsync();

                        // Show progress form
                        var progressForm = new ProgressForm(fileName, fileSize);
                        progressForm.Show();

                        using (var fs = File.OpenRead(filePath))
                        {
                            var buffer = new byte[chunkSize];
                            long totalSent = 0;
                            int bytesRead;
                            while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                // Encrypt chunk
                                var nonce = GenerateNonce();
                                var encryptedChunk = EncryptChunk(buffer, bytesRead, sharedKey, nonce);
                                var chunkMessage = $"{Convert.ToBase64String(encryptedChunk)}|{Convert.ToBase64String(nonce)}|{bytesRead}";
                                var chunkBytes = Encoding.UTF8.GetBytes(chunkMessage);
                                await stream.WriteAsync(chunkBytes, 0, chunkBytes.Length);
                                await stream.FlushAsync();

                                totalSent += bytesRead;
                                progressForm.UpdateProgress(totalSent);
                            }
                        }

                        progressForm.Close();
                        Logger.Log($"Large file sent successfully to {contactIP}: {filePath}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Large file send error: {ex.Message}");
                    return false;
                }
            }
        }

        private byte[] EncryptChunk(byte[] data, int length, byte[] key, byte[] nonce)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = nonce;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        private async Task ReceiveLargeFileAsync(NetworkStream stream, string filePath, long fileSize, int chunkSize, byte[] sharedKey)
        {
            using (var fs = File.Create(filePath))
            {
                var progressForm = new ProgressForm(Path.GetFileName(filePath), fileSize);
                progressForm.Show();
                long totalRead = 0;

                while (totalRead < fileSize)
                {
                    var buffer = new byte[4096];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    var chunkMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var chunkParts = chunkMessage.Split('|');
                    var encryptedChunk = Convert.FromBase64String(chunkParts[0]);
                    var nonce = Convert.FromBase64String(chunkParts[1]);
                    var chunkLength = int.Parse(chunkParts[2]);

                    var decryptedChunk = DecryptChunk(encryptedChunk, sharedKey, nonce);
                    await fs.WriteAsync(decryptedChunk, 0, chunkLength);
                    totalRead += chunkLength;

                    progressForm.UpdateProgress(totalRead);
                }

                progressForm.Close();
            }

            Logger.Log($"Large file received successfully: {filePath}");
        }

        private byte[] DecryptChunk(byte[] cipherText, byte[] key, byte[] nonce)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = nonce;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(cipherText, 0, cipherText.Length);
                    cs.Flush();
                    return ms.ToArray();
                }
            }
        }

        private async void TrySendBufferedMessagesAsync()
        {
            var messages = bufferManager.GetBuffer();
            Logger.Log($"Attempting to send {messages.Count} buffered messages");
            foreach (var msg in messages.ToList())
            {
                bool success = msg.Message.StartsWith("FILE") || msg.Message.StartsWith("IMAGE") ?
                    await SendFileAsync(msg.ContactIP, msg.Message, msg.Message.Split('|')[2]) :
                    await SendTcpMessageAsync(msg.ContactIP, msg.Message);
                if (success)
                {
                    bufferManager.RemoveFromBuffer(msg);
                    Logger.Log($"Buffered message sent to {msg.ContactIP}");
                }
                else
                {
                    var contact = contactIPs.FirstOrDefault(x => x.Value == msg.ContactIP).Key ?? msg.ContactIP;
                    UpdateHistory(contact, $"[SYSTEM] Failed to send buffered message to {contact}.", MessageType.Text, isReceived: false);
                    Logger.Log($"Failed to send buffered message to {msg.ContactIP}: {msg.Message}");
                }
            }
        }

        private void UpdateStatusAndIP()
        {
            lblStatus.Text = $"Status: {myStatus}";
            lblIP.Text = $"IP: {myIP}";
            lblUserInfo.Text = $"User: {myName} ({myLogin})";
            Logger.Log($"Updated UI: Status={myStatus}, IP={myIP}, User={myLogin} ({myName})");
        }

        private void UpdateSendControlsState()
        {
            bool isContactSelected = lstContacts.SelectedItems.Count > 0;
            bool isSelfSelected = isContactSelected && lstContacts.SelectedItems[0].Text.StartsWith(myLogin);
            btnSend.Enabled = isContactSelected && !isSelfSelected;
            btnSendFile.Enabled = isContactSelected && !isSelfSelected;
            txtMessage.Enabled = isContactSelected && !isSelfSelected;
            Logger.Log($"Send controls state updated: {(isContactSelected && !isSelfSelected)}, Selected={(lstContacts.SelectedItems.Count > 0 ? lstContacts.SelectedItems[0].Text : "None")}");
        }

        private void cmbStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            myStatus = cmbStatus.SelectedItem.ToString();
            UpdateStatusAndIP();
            SaveSettings();
            UpdateContactList();
            Logger.Log($"Status changed to {myStatus}");
        }

        private void lstContacts_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSendControlsState();
            if (lstContacts.SelectedItems.Count > 0)
            {
                var contact = lstContacts.SelectedItems[0].Tag as string;
                if (!string.IsNullOrEmpty(contact))
                {
                    UpdateHistoryDisplay(contact);
                    blinkingContacts.Remove(contact);
                    Logger.Log($"Selected contact: {lstContacts.SelectedItems[0].Text}");
                }
                else
                {
                    Logger.Log($"No valid login in Tag for: {lstContacts.SelectedItems[0].Text}");
                    MessageBox.Show("Ошибка: не удалось определить логин контакта.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                Logger.Log("No contact selected");
            }
        }

        private string ExtractLoginFromContactText(string contactText)
        {
            try
            {
                // Предполагаем формат: "login (name, status)"
                var parts = contactText.Split(' ');
                if (parts.Length > 0)
                {
                    var login = parts[0].Trim();
                    if (!string.IsNullOrWhiteSpace(login))
                    {
                        return login;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error extracting login from '{contactText}': {ex.Message}");
                return null;
            }
        }

        private void UpdateContactList()
        {
            for (int i = 0; i < lstContacts.Items.Count; i++)
            {
                var item = lstContacts.Items[i];
                var login = item.Text.Split(' ')[0];
                if (login == myLogin)
                {
                    item.Text = $"{myLogin} ({myName}, {myStatus})";
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            cancellationTokenSource.Cancel();

            SaveSettings();
            bufferManager?.SaveBuffer();
            if (udpListener != null)
            {
                udpListener.Close();
                udpListener.Dispose();
                Logger.Log("UDP listener closed");
            }
            if (udpSender != null)
            {
                udpSender.Close();
                udpSender.Dispose();
                Logger.Log("UDP sender closed");
            }
            if (tcpListener != null)
            {
                tcpListener.Stop();
                Logger.Log("TCP listener stopped");
            }
            //blinkTimer?.Stop();
            Logger.Log("Application closing");
        }

        private void SaveSettings()
        {
            var settings = new Dictionary<string, string>
            {
                { "login", myLogin },
                { "name", myName },
                { "status", myStatus }
            };
            try
            {
                File.WriteAllText(SettingsFile, Newtonsoft.Json.JsonConvert.SerializeObject(settings));
                Logger.Log("Settings saved successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error saving settings: {ex.Message}");
                MessageBox.Show($"Error saving settings: {ex.Message}");
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Logger.Log("Exit button clicked");
            SaveSettings();
            bufferManager.SaveBuffer();
            Application.Exit();
        }

        private void btnOpenSettingsFolder_Click(object sender, EventArgs e)
        {
            try
            {
                Logger.Log("Opening settings folder");
                Process.Start("explorer.exe", AppDataPath);
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
                    if (File.Exists(SettingsFile))
                    {
                        File.Delete(SettingsFile);
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

        private void btnViewLogs_Click(object sender, EventArgs e)
        {
            try
            {
                var logFile = Path.Combine(AppDataPath, "logs", "log.txt");
                Process.Start("notepad.exe", logFile);
                Logger.Log("Opened log file successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error opening log file: {ex.Message}");
                MessageBox.Show($"Failed to open log file: {ex.Message}");
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (var form = new SettingsForm(myLogin, myName, AppDataPath))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    if (myLogin != form.NewLogin || myName != form.NewName)
                    {
                        myLogin = form.NewLogin;
                        myName = form.NewName;
                        SaveSettings();
                        UpdateStatusAndIP();
                        UpdateContactList();
                        Logger.Log($"User settings updated: Login={myLogin}, Name={myName}");
                    }
                    if (form.SelectedIP != null && form.SelectedIP != myIP)
                    {
                        myIP = form.SelectedIP;
                        InitializeNetwork();
                        Logger.Log($"Network reinitialized with IP: {myIP}");
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

        private async void btnCreateGroup_Click(object sender, EventArgs e)
        {
            Logger.Log("Creating group");
            using (var form = new GroupCreationForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    var groupID = form.GroupID;
                    var members = form.SelectedMembers;
                    var groupKey = GenerateGroupKey();
                    groupKeys[groupID] = groupKey;
                    Logger.Log($"Group {groupID} created successfully with members: {string.Join(",", members)}");

                    foreach (var member in members)
                    {
                        string contactIP = contactIPs.ContainsKey(member) ? contactIPs[member] : member;
                        if (!sharedKeys.ContainsKey(member))
                        {
                            Logger.Log($"No shared key found for {member}, attempting key exchange.");
                            var sharedKey = await ExchangeKeysWithContactAsync(contactIP);
                            if (sharedKey == null)
                            {
                                Logger.Log($"Failed to establish connection with {member} (IP: {contactIP})");
                                MessageBox.Show($"Failed to establish connection with {member}");
                                continue;
                            }
                        }

                        var memberSharedKey = sharedKeys[member];
                        var nonce = GenerateNonce();
                        var groupKeyString = Convert.ToBase64String(groupKey);
                        var encryptedGroupKey = Encrypt(groupKeyString, memberSharedKey, nonce);
                        var message = $"GROUP_KEY|{groupID}|{Convert.ToBase64String(encryptedGroupKey)}|{Convert.ToBase64String(nonce)}|{Convert.ToBase64String(new byte[16])}|{myLogin}";
                        bool sent = await SendTcpMessageAsync(contactIP, message);
                        if (!sent)
                        {
                            bufferManager.AddToBuffer(contactIP, message);
                            Logger.Log($"Group key for {member} added to buffer due to send failure");
                            MessageBox.Show($"Group key for {member} added to buffer due to send failure.");
                        }
                    }
                }
                else
                {
                    Logger.Log("Group creation cancelled");
                }
            }
        }

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
            var isBlinking = blinkingContacts.Contains(login) && (DateTime.Now.Second % 2 == 0);
            e.DrawBackground();
            using (var brush = new SolidBrush(isBlinking ? Color.Yellow : Color.Gray))
            {
                e.Graphics.DrawImage(statusIcons.Images[status], e.Bounds.Left, e.Bounds.Top);
                e.Graphics.DrawString(text, new Font("Segoe UI Emoji", 9), brush, e.Bounds.Left + 20, e.Bounds.Top);
            }
            e.DrawFocusRectangle();
        }
    }
}