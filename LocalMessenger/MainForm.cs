using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LocalMessenger
{
    public partial class MainForm : Form
    {
        private readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalMessenger");
        private readonly string AttachmentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalMessenger", "attachments");
        private readonly string HistoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalMessenger", "history");
        private readonly string SettingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalMessenger", "settings.json");
        private readonly UdpClient udpClient;
        private readonly TcpListener tcpListener;
        private readonly ECDiffieHellmanCng myECDH;
        private Dictionary<string, string> contacts = new Dictionary<string, string>();
        private Dictionary<string, byte[]> groupKeys = new Dictionary<string, byte[]>();
        private Dictionary<string, byte[]> contactPublicKeys = new Dictionary<string, byte[]>();
        private string myLogin;
        private string myName;
        private string myStatus = "Online";
        private readonly TrayIconManager trayIconManager;
        private readonly HistoryManager historyManager;

    

        public MainForm()
        {
            InitializeComponent();
            InitializeDirectories();
            LoadSettings();
            InitializeNetwork();
            StartUdpBroadcast();
            StartTcpServer();

            // В конструкторе:
            historyManager = new HistoryManager(AppDataPath, myEncryptionKey); // myEncryptionKey — ваш ключ шифрования
            trayIconManager = new TrayIconManager(this);
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
        }

        private async void StartUdpBroadcast()
        {
            while (true)
            {
                try
                {
                    var data = $"{myLogin}|{myName}|{myStatus}|{GetMyPublicKey()}";
                    var bytes = Encoding.UTF8.GetBytes(data);
                    await udpClient.SendAsync(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, 11000));
                    await Task.Delay(5000); // Heartbeat every 5 seconds
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка широковещания: {ex.Message}");
                }
            }
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

        private byte[] GetMyPublicKey()
        {
            myECDH = new ECDiffieHellmanCng();
            return myECDH.PublicKey.ToByteArray();
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

                if (parts[0] == "KEY_EXCHANGE")
                {
                    var contactLogin = parts[1];
                    var contactPublicKey = Convert.FromBase64String(parts[2]);
                    contactPublicKeys[contactLogin] = contactPublicKey;
                    var sharedKey = myECDH.DeriveKeyMaterial(CngKey.Import(contactPublicKey, CngKeyBlobFormat.EccPublicBlob));
                    // Сохранить sharedKey для дальнейшего использования
                }
                else if (parts[0] == "MESSAGE")
                {
                    var sender = parts[1];
                    var encryptedMessage = Convert.FromBase64String(parts[2]);
                    var nonce = Convert.FromBase64String(parts[3]);
                    var signature = Convert.FromBase64String(parts[4]);

                    // Расшифровка
                    var sharedKey = myECDH.DeriveKeyMaterial(CngKey.Import(contactPublicKeys[sender], CngKeyBlobFormat.EccPublicBlob));
                    var decrypted = Decrypt(encryptedMessage, sharedKey, nonce, signature);

                    // Обновить историю
                    UpdateHistory(sender, decrypted, isReceived: true);
                }
                else if (parts[0] == "GROUP_MESSAGE")
                {
                    var groupID = parts[1];
                    var encryptedMessage = Convert.FromBase64String(parts[2]);
                    var nonce = Convert.FromBase64String(parts[3]);
                    var signature = Convert.FromBase64String(parts[4]);

                    var decrypted = Decrypt(encryptedMessage, groupKeys[groupID], nonce, signature);
                    UpdateGroupHistory(groupID, decrypted, isReceived: true);
                }
            }
        }

        private string Decrypt(byte[] cipherText, byte[] key, byte[] nonce, byte[] signature)
        {
            using (var aes = new AesGcm(key))
            {
                var decrypted = new byte[cipherText.Length];
                var tag = new byte[AesGcm.TagByteSizes.MaxSize];
                Buffer.BlockCopy(signature, 0, tag, 0, tag.Length);

                aes.Decrypt(nonce, cipherText, tag, decrypted);
                return Encoding.UTF8.GetString(decrypted);
            }
        }

        private void UpdateHistory(string contact, string message, bool isReceived)
        {
            // Логика обновления интерфейса и сохранения в файл
        }

        private void UpdateGroupHistory(string groupID, string message, bool isReceived)
        {
            // Логика обновления истории группы
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            var selectedContact = lstContacts.SelectedItem?.ToString();
            if (selectedContact != null && txtMessage.Text.Length > 0)
            {
                var sharedKey = myECDH.DeriveKeyMaterial(CngKey.Import(contactPublicKeys[selectedContact], CngKeyBlobFormat.EccPublicBlob));
                var nonce = GenerateNonce();
                var encrypted = Encrypt(txtMessage.Text, sharedKey, nonce);

                // Отправка через TCP
                SendTcpMessage(selectedContact, $"MESSAGE|{myLogin}|{Convert.ToBase64String(encrypted)}|{Convert.ToBase64String(nonce)}|{Convert.ToBase64String(signature)}");
                UpdateHistory(selectedContact, txtMessage.Text, isReceived: false);
                txtMessage.Clear();
            }
        }

        private byte[] Encrypt(string plainText, byte[] key, byte[] nonce)
        {
            using (var aes = new AesGcm(key))
            {
                var cipherText = new byte[plainText.Length];
                var tag = new byte[AesGcm.TagByteSizes.MaxSize];
                aes.Encrypt(nonce, Encoding.UTF8.GetBytes(plainText), cipherText, tag);
                return cipherText;
            }
        }

        private byte[] GenerateNonce()
        {
            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
            RandomNumberGenerator.Fill(nonce);
            return nonce;
        }

        private void SendTcpMessage(string contactIP, string message)
        {
            var client = new TcpClient();
            client.Connect(contactIP, 12000);
            var stream = client.GetStream();
            var bytes = Encoding.UTF8.GetBytes(message);
            stream.Write(bytes, 0, bytes.Length);
        }

        private void cmbStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            myStatus = cmbStatus.SelectedItem.ToString();
            // Обновить статус в настройках
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

                    // Отправить ключ каждому участнику через ECDH
                    foreach (var member in members)
                    {
                        var sharedKey = myECDH.DeriveKeyMaterial(CngKey.Import(contactPublicKeys[member], CngKeyBlobFormat.EccPublicBlob));
                        var encryptedGroupKey = Encrypt(groupKey, sharedKey, GenerateNonce());
                        SendTcpMessage(member, $"GROUP_KEY|{groupID}|{Convert.ToBase64String(encryptedGroupKey)}");
                    }
                }
            }
        }

        private byte[] GenerateGroupKey()
        {
            var key = new byte[32]; // 256-bit key
            RandomNumberGenerator.Fill(key);
            return key;
        }
    }
}