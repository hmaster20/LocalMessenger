using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace LocalMessenger
{
    public partial class MainForm : Form
    {
        private string AppDataPath;
        private string AttachmentsPath;
        private string HistoryPath;
        private string SettingsFile;

        private UdpClient udpClient;
        private TcpListener tcpListener;
        private ECDiffieHellmanCng myECDH;
        private Dictionary<string, byte[]> contactPublicKeys = new Dictionary<string, byte[]>();
        private Dictionary<string, byte[]> sharedKeys = new Dictionary<string, byte[]>();
        private Dictionary<string, byte[]> groupKeys = new Dictionary<string, byte[]>();

        private string myLogin;
        private string myName;
        private string myStatus = "Online";
        private string myIP = GetLocalIPAddress();
        private HistoryManager historyManager;
        private MessageBufferManager bufferManager;
        private bool isLoggingOut = false; // Флаг для предотвращения сохранения настроек при выходе

        public MainForm()
        {
            InitializeComponent();
            InitializePaths();
            InitializeDirectories();
            LoadSettings();
            InitializeNetwork();
            historyManager = new HistoryManager(AppDataPath, GenerateEncryptionKey());
            bufferManager = new MessageBufferManager(AppDataPath);
            new TrayIconManager(this);
            StartUdpBroadcast();
            StartTcpServer();
            UpdateStatusAndIP();
            TrySendBufferedMessagesAsync();
            UpdateSendControlsState(); // Инициализация состояния элементов отправки
        }

        private void InitializePaths()
        {
            AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalMessenger");
            AttachmentsPath = Path.Combine(AppDataPath, "attachments");
            HistoryPath = Path.Combine(AppDataPath, "history");
            SettingsFile = Path.Combine(AppDataPath, "settings.json");
        }

        private void InitializeDirectories()
        {
            Directory.CreateDirectory(AppDataPath);
            Directory.CreateDirectory(AttachmentsPath);
            Directory.CreateDirectory(HistoryPath);
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
                    }
                    else
                    {
                        ShowRegistrationForm();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки настроек: {ex.Message}");
                    ShowRegistrationForm();
                }
            }
            else
            {
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
                }
                else
                {
                    Application.Exit();
                }
            }
        }

        private void InitializeNetwork()
        {
            udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;
            tcpListener = new TcpListener(IPAddress.Any, 12000);
            InitializeECDH();
        }

        private void InitializeECDH()
        {
            myECDH = new ECDiffieHellmanCng();
            myECDH.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            myECDH.HashAlgorithm = CngAlgorithm.Sha256;
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapter found!");
        }

        private async void StartUdpBroadcast()
        {
            while (true)
            {
                try
                {
                    var publicKey = GetMyPublicKey();
                    var data = $"HELLO|{myLogin}|{myName}|{myStatus}|{Convert.ToBase64String(publicKey)}";
                    var bytes = Encoding.UTF8.GetBytes(data);
                    await udpClient.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, 11000));
                    await Task.Delay(5000);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка широковещания: {ex.Message}");
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
                while (true)
                {
                    var client = await tcpListener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сервера: {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();
                var buffer = new byte[4096];
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var parts = message.Split('|');

                if (parts[0] == "HELLO")
                {
                    var sender = parts[1];
                    var name = parts[2];
                    var status = parts[3];
                    var publicKey = Convert.FromBase64String(parts[4]);
                    contactPublicKeys[sender] = publicKey;
                    if (!lstContacts.Items.Contains(sender))
                    {
                        lstContacts.Items.Add(sender);
                    }
                }
                else if (parts[0] == "KEY_EXCHANGE")
                {
                    var sender = parts[1];
                    var contactPublicKey = Convert.FromBase64String(parts[2]);
                    contactPublicKeys[sender] = contactPublicKey;

                    var publicKey = GetMyPublicKey();
                    var response = $"KEY_EXCHANGE_RESPONSE|{myLogin}|{Convert.ToBase64String(publicKey)}";
                    var bytes = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(bytes, 0, bytes.Length);

                    var sharedKey = myECDH.DeriveKeyMaterial(CngKey.Import(contactPublicKey, CngKeyBlobFormat.EccPublicBlob));
                    sharedKeys[sender] = sharedKey;
                }
                else if (parts[0] == "KEY_EXCHANGE_RESPONSE")
                {
                    var sender = parts[1];
                    var contactPublicKey = Convert.FromBase64String(parts[2]);
                    contactPublicKeys[sender] = contactPublicKey;

                    var sharedKey = myECDH.DeriveKeyMaterial(CngKey.Import(contactPublicKey, CngKeyBlobFormat.EccPublicBlob));
                    sharedKeys[sender] = sharedKey;
                }
                else if (parts[0] == "MESSAGE")
                {
                    var sender = parts[1];
                    var encryptedMessage = Convert.FromBase64String(parts[2]);
                    var nonce = Convert.FromBase64String(parts[3]);
                    var tag = Convert.FromBase64String(parts[4]);

                    var decrypted = Decrypt(encryptedMessage, sharedKeys[sender], nonce, tag);
                    UpdateHistory(sender, decrypted, isReceived: true);
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
                }
                else if (parts[0] == "GROUP_KEY")
                {
                    var groupID = parts[1];
                    var encryptedGroupKey = Convert.FromBase64String(parts[2]);
                    var nonce = Convert.FromBase64String(parts[3]);
                    var tag = Convert.FromBase64String(parts[4]);
                    var sender = parts[5];

                    if (sharedKeys.ContainsKey(sender))
                    {
                        var decryptedGroupKeyString = Decrypt(encryptedGroupKey, sharedKeys[sender], nonce, tag);
                        groupKeys[groupID] = Convert.FromBase64String(decryptedGroupKeyString);
                    }
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
                Content = message
            };
            historyManager.AddMessage(contact, msg);

            if (lstContacts.SelectedItem?.ToString() == contact)
            {
                UpdateHistoryDisplay(contact);
            }
        }

        private void UpdateGroupHistory(string groupID, string message, bool isReceived)
        {
            // Реализация для групповых сообщений
        }

        private void UpdateHistoryDisplay(string contact)
        {
            rtbHistory.Clear();
            var messages = historyManager.LoadMessages(contact);
            foreach (var msg in messages)
            {
                rtbHistory.AppendText($"[{msg.Timestamp}] {msg.Sender}: {msg.Content}\n");
            }
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            var selectedContact = lstContacts.SelectedItem?.ToString();
            if (selectedContact != null && txtMessage.Text.Length > 0)
            {
                byte[] sharedKey = null;
                if (!sharedKeys.ContainsKey(selectedContact))
                {
                    sharedKey = await ExchangeKeysWithContactAsync(selectedContact);
                    if (sharedKey == null)
                    {
                        MessageBox.Show($"Не удалось установить соединение с {selectedContact}");
                        return;
                    }
                }
                else
                {
                    sharedKey = sharedKeys[selectedContact];
                }

                var nonce = GenerateNonce();
                var encrypted = Encrypt(txtMessage.Text, sharedKey, nonce);

                var message = $"MESSAGE|{myLogin}|{Convert.ToBase64String(encrypted)}|{Convert.ToBase64String(nonce)}|{Convert.ToBase64String(new byte[16])}";
                bool sent = await SendTcpMessageAsync(selectedContact, message);

                if (sent)
                {
                    UpdateHistory(selectedContact, txtMessage.Text, isReceived: false);
                    txtMessage.Clear();
                }
                else
                {
                    bufferManager.AddToBuffer(selectedContact, message);
                    MessageBox.Show($"Сообщение для {selectedContact} добавлено в буфер из-за ошибки отправки.");
                }
            }
        }

        private async Task<byte[]> ExchangeKeysWithContactAsync(string contactIP)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync(contactIP, 12000);
                    var stream = client.GetStream();

                    var publicKey = GetMyPublicKey();
                    var message = $"KEY_EXCHANGE|{myLogin}|{Convert.ToBase64String(publicKey)}";
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await stream.WriteAsync(bytes, 0, bytes.Length);

                    var buffer = new byte[4096];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var parts = response.Split('|');

                    if (parts[0] == "KEY_EXCHANGE_RESPONSE")
                    {
                        var sender = parts[1];
                        var contactPublicKey = Convert.FromBase64String(parts[2]);
                        contactPublicKeys[sender] = contactPublicKey;

                        var sharedKey = myECDH.DeriveKeyMaterial(CngKey.Import(contactPublicKey, CngKeyBlobFormat.EccPublicBlob));
                        sharedKeys[sender] = sharedKey;
                        return sharedKey;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обмена ключами: {ex.Message}");
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
                    await client.ConnectAsync(contactIP, 12000);
                    using (var stream = client.GetStream())
                    {
                        var bytes = Encoding.UTF8.GetBytes(message);
                        await stream.WriteAsync(bytes, 0, bytes.Length);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка отправки сообщения: {ex.Message}");
                    return false;
                }
            }
        }

        private async void TrySendBufferedMessagesAsync()
        {
            var messages = bufferManager.GetBuffer();
            foreach (var msg in messages.ToList())
            {
                bool success = await SendTcpMessageAsync(msg.ContactIP, msg.Message);
                if (success)
                {
                    bufferManager.RemoveFromBuffer(msg);
                }
                else
                {
                    MessageBox.Show($"Не удалось отправить буферизованное сообщение для {msg.ContactIP}: {msg.Message}");
                }
            }
        }

        private void UpdateStatusAndIP()
        {
            lblStatus.Text = $"Статус: {myStatus}";
            lblIP.Text = $"IP: {myIP}";
            lblUserInfo.Text = $"Пользователь: {myLogin} ({myName})";
        }

        private void UpdateSendControlsState()
        {
            bool isContactSelected = lstContacts.SelectedItem != null;
            btnSend.Enabled = isContactSelected;
            txtMessage.Enabled = isContactSelected;
        }

        private void cmbStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            myStatus = cmbStatus.SelectedItem.ToString();
            UpdateStatusAndIP();
            SaveSettings();
        }

        private void lstContacts_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSendControlsState(); // Обновляем состояние элементов при изменении выбора
            if (lstContacts.SelectedItem != null)
            {
                UpdateHistoryDisplay(lstContacts.SelectedItem.ToString());
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!isLoggingOut)
            {
                SaveSettings();
            }
            bufferManager.SaveBuffer();
            if (udpClient != null)
            {
                udpClient.Close();
                udpClient.Dispose();
            }
            if (tcpListener != null)
            {
                tcpListener.Stop();
            }
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения настроек: {ex.Message}");
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            try
            {
                isLoggingOut = true; // Устанавливаем флаг, чтобы не сохранять настройки при закрытии
                if (File.Exists(SettingsFile))
                {
                    File.Delete(SettingsFile);
                }
                MessageBox.Show("Данные пользователя удалены. Приложение будет закрыто.");
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении данных: {ex.Message}");
            }
        }

        private byte[] GenerateEncryptionKey()
        {
            var key = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            notifyIcon.Visible = false;
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
            }
        }

        private async void btnCreateGroup_Click(object sender, EventArgs e)
        {
            using (var form = new GroupCreationForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    var groupID = form.GroupID;
                    var members = form.SelectedMembers;
                    var groupKey = GenerateGroupKey();
                    groupKeys[groupID] = groupKey;

                    foreach (var member in members)
                    {
                        if (!sharedKeys.ContainsKey(member))
                        {
                            var sharedKey = await ExchangeKeysWithContactAsync(member);
                            if (sharedKey == null)
                            {
                                MessageBox.Show($"Не удалось установить соединение с {member}");
                                continue;
                            }
                        }

                        var memberSharedKey = sharedKeys[member];
                        var nonce = GenerateNonce();
                        var groupKeyString = Convert.ToBase64String(groupKey);
                        var encryptedGroupKey = Encrypt(groupKeyString, memberSharedKey, nonce);
                        var message = $"GROUP_KEY|{groupID}|{Convert.ToBase64String(encryptedGroupKey)}|{Convert.ToBase64String(nonce)}|{Convert.ToBase64String(new byte[16])}|{myLogin}";
                        bool sent = await SendTcpMessageAsync(member, message);
                        if (!sent)
                        {
                            bufferManager.AddToBuffer(member, message);
                            MessageBox.Show($"Ключ группы для {member} добавлен в буфер из-за ошибки отправки.");
                        }
                    }
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
            return key;
        }
    }
}