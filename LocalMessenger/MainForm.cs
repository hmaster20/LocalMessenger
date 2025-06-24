using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

namespace LocalMessenger
{
    public partial class MainForm : Form
    {
        private string AppDataPath;
        private string AttachmentsPath;
        private string HistoryPath;
        private string SettingsFile;

        private UdpClient udpListener; // Для приема сообщений
        private UdpClient udpSender; // Для отправки сообщений
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
        private byte[] encryptionKey; // Ключ для шифрования истории
        private Timer blinkTimer; // Таймер для мерцания
        private HashSet<string> blinkingContacts = new HashSet<string>(); // Контакты с новыми сообщениями

        public MainForm()
        {
            InitializeComponent();
            InitializeBlinkTimer();
            Logger.Log($"Application started. Session initialized for IP: {myIP}");
            InitializePaths();
            InitializeDirectories();
            encryptionKey = GenerateEncryptionKey();
            LoadSettings();
            InitializeNetwork();
            historyManager = new HistoryManager(AppDataPath, encryptionKey);
            bufferManager = new MessageBufferManager(AppDataPath);
            new TrayIconManager(this);
            AddCurrentUserToContacts();
            StartUdpBroadcast();
            StartUdpListener();
            StartTcpServer();
            UpdateStatusAndIP();
            TrySendBufferedMessagesAsync();
            UpdateSendControlsState();
            LoadAllHistories();
            ConfigureMessageBox();
        }

        private void InitializeBlinkTimer()
        {
            blinkTimer = new Timer { Interval = 1000 }; // Мерцание раз в секунду
            blinkTimer.Tick += BlinkTimer_Tick;
            blinkTimer.Start();
        }

        private void BlinkTimer_Tick(object sender, EventArgs e)
        {
            foreach (var contact in blinkingContacts.ToList())
            {
                int index = lstContacts.Items.IndexOf(lstContacts.Items.Cast<string>().FirstOrDefault(i => i.StartsWith(contact)));
                if (index >= 0)
                {
                    lstContacts.SetSelected(index, lstContacts.SelectedIndex == index); // Сохраняем выбор
                    lstContacts.Items[index] = lstContacts.Items[index]; // Освежаем элемент
                    lstContacts.Invalidate(); // Перерисовываем
                }
            }
        }

        private void ConfigureMessageBox()
        {
            txtMessage.KeyDown += txtMessage_KeyDown;
            rtbHistory.WordWrap = true;
            rtbHistory.ScrollBars = RichTextBoxScrollBars.Vertical;
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                e.SuppressKeyPress = true; // Предотвращаем перенос строки
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

        private void LoadSettings()
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
                    Logger.Log($"Error loading settings: {ex.Message}");
                    MessageBox.Show($"Error loading settings: {ex.Message}");
                    ShowRegistrationForm();
                }
            }
            else
            {
                Logger.Log("Settings file not found. Showing registration form.");
                ShowRegistrationForm();
            }
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
            myECDH = new ECDiffieHellmanCng();
            myECDH.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            myECDH.HashAlgorithm = CngAlgorithm.Sha256;
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
            if (!lstContacts.Items.Contains($"{myLogin} ({myName}, {myStatus})"))
            {
                lstContacts.Items.Add($"{myLogin} ({myName}, {myStatus})");
                Logger.Log($"Added current user to contacts: {myLogin}");
            }
        }

        private async void StartUdpBroadcast()
        {
            Logger.Log("Starting UDP broadcast for user discovery");
            while (true)
            {
                try
                {
                    var publicKey = GetMyPublicKey();
                    var data = $"HELLO|{myLogin}|{myName}|{myStatus}|{Convert.ToBase64String(publicKey)}";
                    var bytes = Encoding.UTF8.GetBytes(data);
                    await udpSender.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, 11000));
                    Logger.Log($"Sent HELLO broadcast from {myIP}: {data}");
                    await Task.Delay(2000);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Broadcast error: {ex.Message}");
                    MessageBox.Show($"Broadcast error: {ex.Message}");
                }
            }
        }

        private async void StartUdpListener()
        {
            Logger.Log("Starting UDP listener on port 11000");
            while (true)
            {
                try
                {
                    var result = await udpListener.ReceiveAsync();
                    var message = Encoding.UTF8.GetString(result.Buffer);
                    var remoteIP = result.RemoteEndPoint.Address.ToString();
                    Logger.Log($"Received UDP message from {remoteIP}: {message}");

                    var parts = message.Split('|');
                    if (parts.Length == 5 && parts[0] == "HELLO")
                    {
                        var sender = parts[1];
                        var name = parts[2];
                        var status = parts[3];
                        var publicKey = Convert.FromBase64String(parts[4]);

                        if (sender != myLogin)
                        {
                            contactPublicKeys[sender] = publicKey;
                            contactIPs[sender] = remoteIP;
                            var contactString = $"{sender} ({name}, {status})";
                            int existingIndex = lstContacts.Items.IndexOf(lstContacts.Items.Cast<string>().FirstOrDefault(i => i.StartsWith(sender)));
                            if (existingIndex >= 0)
                            {
                                lstContacts.Items[existingIndex] = contactString;
                                Logger.Log($"Updated contact: {sender} (Name: {name}, Status: {status}, IP: {remoteIP})");
                            }
                            else
                            {
                                lstContacts.Items.Add(contactString);
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
                    Logger.Log($"UDP listener error: {ex.Message}");
                }
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
                        Logger.Log($"Shared key established with {sender}");
                    }
                    else if (parts[0] == "KEY_EXCHANGE_RESPONSE")
                    {
                        var sender = parts[1];
                        var contactPublicKey = Convert.FromBase64String(parts[2]);
                        contactPublicKeys[sender] = contactPublicKey;

                        var sharedKey = myECDH.DeriveKeyMaterial(CngKey.Import(contactPublicKey, CngKeyBlobFormat.EccPublicBlob));
                        sharedKeys[sender] = sharedKey;
                        Logger.Log($"Shared key established with {sender}");
                    }
                    else if (parts[0] == "MESSAGE")
                    {
                        var sender = parts[1];
                        var encryptedMessage = Convert.FromBase64String(parts[2]);
                        var nonce = Convert.FromBase64String(parts[3]);
                        var tag = Convert.FromBase64String(parts[4]);

                        var decrypted = Decrypt(encryptedMessage, sharedKeys[sender], nonce, tag);
                        UpdateHistory(sender, decrypted, isReceived: true);
                        if (lstContacts.SelectedItem?.ToString().StartsWith(sender) != true)
                        {
                            blinkingContacts.Add(sender);
                        }
                        Logger.Log($"Received MESSAGE from {sender}: {decrypted}");
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

        private string Decrypt(byte[] cipherText, byte[] key, byte[] nonce, byte[] tag)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = nonce;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                {
                    var decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }

        private byte[] Encrypt(string plainText, byte[] key, byte[] nonce)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = nonce;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(Encoding.UTF8.GetBytes(plainText), 0, plainText.Length);
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

        private void UpdateHistory(string contact, string message, bool isReceived)
        {
            var msg = new Message
            {
                Sender = isReceived ? contact : myLogin,
                Content = message,
                Timestamp = DateTime.Now
            };
            historyManager.AddMessage(contact, msg);

            if (lstContacts.SelectedItem?.ToString().StartsWith(contact) == true)
            {
                UpdateHistoryDisplay(contact);
            }
            Logger.Log($"Updated history for {contact}: {(isReceived ? "Received" : "Sent")} - {message}");
        }

        private void UpdateGroupHistory(string groupID, string message, bool isReceived)
        {
            Logger.Log($"Updated group history for {groupID}: {(isReceived ? "Received" : "Sent")} - {message}");
        }

        private void UpdateHistoryDisplay(string contact)
        {
            rtbHistory.Clear();
            var messages = historyManager.LoadMessages(contact);
            foreach (var msg in messages)
            {
                var line = $"[{msg.Timestamp:dd.MM.yyyy HH:mm:ss}] {msg.Sender}: {msg.Content}";
                rtbHistory.AppendText(line + Environment.NewLine);
            }
            rtbHistory.ScrollToCaret();
            Logger.Log($"Displayed history for {contact}");
        }

        private void LoadAllHistories()
        {
            foreach (var contact in contactIPs.Keys)
            {
                historyManager.LoadMessages(contact); // Предзагрузка для кэширования
            }
            Logger.Log("All histories loaded");
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            var selectedContact = lstContacts.SelectedItem?.ToString();
            if (selectedContact == null) return;
            var contactLogin = selectedContact.Split(' ')[0]; // Извлекаем логин
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
                    UpdateHistory(contactLogin, txtMessage.Text, isReceived: false);
                    txtMessage.Clear();
                    Logger.Log($"Message sent to {contactLogin} (IP: {contactIP}): {txtMessage.Text}");
                }
                else
                {
                    bufferManager.AddToBuffer(contactIP, message);
                    UpdateHistory(contactLogin, $"[SYSTEM] Message to {contactLogin} buffered due to send failure.", isReceived: false);
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
                    Logger.Log($"Sent KEY_EXCHANGE to {contactIP}");

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

        private async void TrySendBufferedMessagesAsync()
        {
            var messages = bufferManager.GetBuffer();
            Logger.Log($"Attempting to send {messages.Count} buffered messages");
            foreach (var msg in messages.ToList())
            {
                bool success = await SendTcpMessageAsync(msg.ContactIP, msg.Message);
                if (success)
                {
                    bufferManager.RemoveFromBuffer(msg);
                    Logger.Log($"Buffered message sent to {msg.ContactIP}");
                }
                else
                {
                    var contact = contactIPs.FirstOrDefault(x => x.Value == msg.ContactIP).Key ?? msg.ContactIP;
                    UpdateHistory(contact, $"[SYSTEM] Failed to send buffered message to {contact}.", isReceived: false);
                    Logger.Log($"Failed to send buffered message to {msg.ContactIP}: {msg.Message}");
                }
            }
        }

        private void UpdateStatusAndIP()
        {
            lblStatus.Text = $"Status: {myStatus}";
            lblIP.Text = $"IP: {myIP}";
            lblUserInfo.Text = $"User: {myLogin} ({myName})";
            Logger.Log($"Updated UI: Status={myStatus}, IP={myIP}, User={myLogin} ({myName})");
        }

        private void UpdateSendControlsState()
        {
            bool isContactSelected = lstContacts.SelectedItem != null;
            bool isSelfSelected = lstContacts.SelectedItem?.ToString().StartsWith(myLogin) == true;
            btnSend.Enabled = isContactSelected && !isSelfSelected;
            txtMessage.Enabled = isContactSelected && !isSelfSelected;
            Logger.Log($"Send controls state updated: Enabled={isContactSelected && !isSelfSelected}, Selected={lstContacts.SelectedItem?.ToString() ?? "None"}");
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
            if (lstContacts.SelectedItem != null)
            {
                var contact = lstContacts.SelectedItem.ToString().Split(' ')[0];
                UpdateHistoryDisplay(contact);
                blinkingContacts.Remove(contact);
            }
            Logger.Log($"Selected contact: {lstContacts.SelectedItem?.ToString() ?? "None"}");
        }

        private void UpdateContactList()
        {
            for (int i = 0; i < lstContacts.Items.Count; i++)
            {
                var item = lstContacts.Items[i].ToString();
                var login = item.Split(' ')[0];
                if (login == myLogin)
                {
                    lstContacts.Items[i] = $"{myLogin} ({myName}, {myStatus})";
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
            bufferManager.SaveBuffer();
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
            blinkTimer.Stop();
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
                "Confirm Account Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                try
                {
                    if (File.Exists(SettingsFile))
                    {
                        File.Delete(SettingsFile);
                        Logger.Log("Account settings deleted");
                    }
                    Logger.Log("Account deletion confirmed. Closing application.");
                    MessageBox.Show("Account deleted. The application will close.");
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
                Logger.Log("Opened log file");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error opening log file: {ex.Message}");
                MessageBox.Show($"Error opening log file: {ex.Message}");
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
                        Logger.Log($"Network reinitialized with new IP: {myIP}");
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

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            notifyIcon.Visible = false;
            Logger.Log("Notification icon double-clicked. Restored.");
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                notifyIcon.Visible = true;
                notifyIcon.Text = "LocalMessenger";
                notifyIcon.BalloonTipText = "LocalMessenger";
                notifyIcon.BalloonTipTitle = "LocalMessenger";
                notifyIcon.ShowBalloonTip(500);
                Logger.Log("Application minimized to tray");
            }
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
                    Logger.Log($"Group created: ID={groupID}, Members={string.Join(",", members)}");

                    foreach (var member in members)
                    {
                        string contactIP = contactIPs.ContainsKey(member) ? contactIPs[member] : member;
                        if (!sharedKeys.ContainsKey(member))
                        {
                            var sharedKey = await ExchangeKeysWithContactAsync(contactIP);
                            if (sharedKey == null)
                            {
                                Logger.Log($"Failed to establish connection with {member}");
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
                            Logger.Log($"Group key for {member} added to buffer");
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
    }
}