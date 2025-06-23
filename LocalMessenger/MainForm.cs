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
        private Dictionary<string, byte[]> groupKeys = new Dictionary<string, byte[]>(); // Исправлено: byte[] вместо string

        private string myLogin;
        private string myName;
        private string myStatus = "Online";
        private string myIP = GetLocalIPAddress();

        //// Элементы интерфейса
        //private ListBox lstContacts;
        //private TextBox txtMessage;
        //private ComboBox cmbStatus;
        //private NotifyIcon notifyIcon;
        //private Button btnSend;
        //private Button btnCreateGroup;

        public MainForm()
        {
            InitializeComponent();
            InitializePaths();
            InitializeDirectories();
            LoadSettings();
            InitializeNetwork();
            new TrayIconManager(this); // Инициализация TrayIcon
            StartUdpBroadcast();
            StartTcpServer();
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
                var json = File.ReadAllText(SettingsFile);
                var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                myLogin = settings["login"];
                myName = settings["name"];
                myStatus = settings.ContainsKey("status") ? settings["status"] : "Online";
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
                    var settings = new Dictionary<string, string>
                    {
                        { "login", myLogin },
                        { "name", myName },
                        { "status", myStatus }
                    };
                    File.WriteAllText(SettingsFile, Newtonsoft.Json.JsonConvert.SerializeObject(settings));
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
                    await Task.Delay(5000); // Heartbeat
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
                    var sender = parts[5]; // Исправлено: правильное получение отправителя

                    if (sharedKeys.ContainsKey(sender))
                    {
                        var decryptedGroupKeyString = Decrypt(encryptedGroupKey, sharedKeys[sender], nonce, tag);
                        groupKeys[groupID] = Convert.FromBase64String(decryptedGroupKeyString); // Исправлено: преобразование string в byte[]
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
            var nonce = new byte[16]; // 128-bit nonce for AES
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(nonce);
            }
            return nonce;
        }

        private void UpdateHistory(string contact, string message, bool isReceived)
        {
            // Логика обновления интерфейса и сохранения в файл
        }

        private void UpdateGroupHistory(string groupID, string message, bool isReceived)
        {
            // Логика обновления истории группы
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            var selectedContact = lstContacts.SelectedItem?.ToString();
            if (selectedContact != null && txtMessage.Text.Length > 0)
            {
                if (!sharedKeys.ContainsKey(selectedContact))
                {
                    await ExchangeKeysWithContact(selectedContact);
                }

                var sharedKey = sharedKeys[selectedContact];
                var nonce = GenerateNonce();
                var encrypted = Encrypt(txtMessage.Text, sharedKey, nonce);

                var message = $"MESSAGE|{myLogin}|{Convert.ToBase64String(encrypted)}|{Convert.ToBase64String(nonce)}|{Convert.ToBase64String(new byte[16])}";
                SendTcpMessage(selectedContact, message);
                UpdateHistory(selectedContact, txtMessage.Text, isReceived: false);
                txtMessage.Clear();
            }
        }

        private async Task<byte[]> ExchangeKeysWithContact(string contactIP)
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

        //private void SendTcpMessage(string contactIP, string message)
        //{
        //    var client = new TcpClient();
        //    client.Connect(contactIP, 12000);
        //    var stream = client.GetStream();
        //    var bytes = Encoding.UTF8.GetBytes(message);
        //    stream.Write(bytes, 0, bytes.Length);
        //}

        private void SendTcpMessage(string contactIP, string message)
        {
            using (var client = new TcpClient())
            {
                try
                {
                    client.Connect(contactIP, 12000);
                    using (var stream = client.GetStream())
                    {
                        var bytes = Encoding.UTF8.GetBytes(message);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка отправки сообщения: {ex.Message}");
                }
            }
        }

        private void cmbStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            myStatus = cmbStatus.SelectedItem.ToString();
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
                notifyIcon.BalloonTipTitle = "Название";
                notifyIcon.ShowBalloonTip(500);

            }
        }

        private void btnCreateGroup_Click(object sender, EventArgs e)
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
                            _ = ExchangeKeysWithContact(member);
                        }

                        var sharedKey = sharedKeys[member];
                        var nonce = GenerateNonce();
                        var groupKeyString = Convert.ToBase64String(groupKey);
                        var encryptedGroupKey = Encrypt(groupKeyString, sharedKey, nonce);
                        var message = $"GROUP_KEY|{groupID}|{Convert.ToBase64String(encryptedGroupKey)}|{Convert.ToBase64String(nonce)}|{Convert.ToBase64String(new byte[16])}|{myLogin}";
                        SendTcpMessage(member, message);
                    }
                }
            }
        }

        private byte[] GenerateGroupKey()
        {
            var key = new byte[32]; // 256-bit group key
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(key);
            }
            return key;
        }


    }
}