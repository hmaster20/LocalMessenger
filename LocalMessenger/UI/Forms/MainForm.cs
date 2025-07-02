using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LocalMessenger.Core.Models;
using LocalMessenger.Core.Network;
using LocalMessenger.Core.Security;
using LocalMessenger.Core.Services;
using LocalMessenger.Network.Tcp;
using LocalMessenger.Network.Udp;
using LocalMessenger.UI.Components;
using LocalMessenger.Utilities;
using Message = LocalMessenger.Core.Models.Message;

namespace LocalMessenger.UI.Forms
{
    public partial class MainForm : Form
    {
        private readonly HistoryManager _historyManager;
        private readonly MessageBufferManager _bufferManager;
        private readonly FileTransfer _fileTransfer;
        private readonly ContactManager _contactManager;
        private readonly MessageHandler _messageHandler;
        private readonly UIManager _uiManager;
        private UdpManager _udpManager;
        private TcpServer _tcpServer;
        private ECDiffieHellmanCng _myECDH;
        private readonly Dictionary<string, byte[]> _contactPublicKeys;
        private readonly Dictionary<string, byte[]> _sharedKeys;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private string _myLogin;
        private string _myName;
        private string _myStatus;
        private string _myIP;
        private byte[] _encryptionKey;

        public MainForm()
        {
            InitializeComponent();
            _contactPublicKeys = new Dictionary<string, byte[]>();
            _sharedKeys = new Dictionary<string, byte[]>();
            _cancellationTokenSource = new CancellationTokenSource();

            InitializeECDH();
            LoadSettings();
            _historyManager = new HistoryManager(Configuration.HistoryPath, _encryptionKey);
            _bufferManager = new MessageBufferManager(Configuration.AppDataPath);
            _fileTransfer = new FileTransfer(_encryptionKey);
            _contactManager = new ContactManager(lstContacts, _myLogin, _myName, _myStatus);
            _messageHandler = new MessageHandler(_historyManager, _bufferManager, _fileTransfer, _sharedKeys, _myECDH, _myLogin, notifyIcon, this);
            _uiManager = new UIManager(this, lstContacts, rtbHistory, txtMessage, lblStatus, lblIP, lblUserInfo, _contactManager, _historyManager, _myLogin);
            _udpManager = new UdpManager(_myLogin, _myName, _myStatus, GetMyPublicKey, _contactManager.HandleUdpMessage);
            _tcpServer = new TcpServer(_messageHandler.HandleKeyExchange,
                (sender, encrypted, nonce, tag) => _messageHandler.HandleMessageAsync(sender, encrypted, nonce, tag, _uiManager.SelectedContact, _uiManager.UpdateHistoryDisplay),
                (sender, fileName, fileSize, nonce, isChunked) => _messageHandler.HandleFileAsync(sender, fileName, fileSize, nonce, isChunked, _uiManager.SelectedContact, _uiManager.UpdateHistoryDisplay));

            new TrayIconManager(this, Properties.Resources.LocalMessenger);
            _uiManager.InitializeUI();
            _uiManager.LoadAllHistories();
            _uiManager.UpdateStatusAndIP(_myIP, _myName, _myLogin, _myStatus);
            _messageHandler.SendBufferedMessagesAsync(null, null, _uiManager.UpdateHistoryDisplay);

            Task.Run(() => _udpManager.StartBroadcastAsync(), _cancellationTokenSource.Token);
            Task.Run(() => _udpManager.StartListenerAsync(), _cancellationTokenSource.Token);
            Task.Run(() => _tcpServer.StartAsync(), _cancellationTokenSource.Token);
        }

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

        private void LoadSettings()
        {
            try
            {
                if (System.IO.File.Exists(Configuration.SettingsFile))
                {
                    var json = System.IO.File.ReadAllText(Configuration.SettingsFile);
                    var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    settings.TryGetValue("Login", out var login);
                    _myLogin = login ?? Environment.UserName;
                    settings.TryGetValue("Name", out var name);
                    _myName = name ?? Environment.UserName;
                    settings.TryGetValue("Status", out var status);
                    _myStatus = status ?? "Online";
                    settings.TryGetValue("EncryptionKey", out var keyBase64);
                    _encryptionKey = !string.IsNullOrEmpty(keyBase64) ? Convert.FromBase64String(keyBase64) : CryptoUtils.GenerateEncryptionKey();
                    cmbStatus.SelectedItem = _myStatus;
                    Logger.Log($"Settings loaded: Login={_myLogin}, Name={_myName}, Status={_myStatus}");
                }
                else
                {
                    _myLogin = Environment.UserName;
                    _myName = Environment.UserName;
                    _myStatus = "Online";
                    _encryptionKey = CryptoUtils.GenerateEncryptionKey();
                    SaveSettings();
                    Logger.Log("No settings file found, initialized with defaults and new encryption key");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading settings: {ex.Message}");
            }
        }

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
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(Configuration.SettingsFile, json);
                Logger.Log("Settings saved successfully");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error saving settings: {ex.Message}");
            }
        }

        public void SaveSettingsPublic()
        {
            SaveSettings();
        }

        public async Task<byte[]> ExchangeKeysWithContactAsync(string contactIP)
        {
            try
            {
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    await client.ConnectAsync(contactIP, 12000);
                    var publicKey = GetMyPublicKey();
                    var message = $"KEY_EXCHANGE|{_myLogin}|{Convert.ToBase64String(publicKey)}";
                    var bytes = System.Text.Encoding.UTF8.GetBytes(message);
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

        public void AddToBuffer(string contactIP, string message)
        {
            _bufferManager.AddToBuffer(contactIP, message);
        }

        public string GetMyIP()
        {
            return _myIP;
        }

        public string GetMyName()
        {
            return _myName;
        }

        private async void btnSend_Click(object s, EventArgs e)
        {
            await _uiManager.HandleSendMessageAsync(_contactManager, _messageHandler, _historyManager, _sharedKeys, _myLogin);
        }

        private async void btnSendFile_Click(object s, EventArgs e)
        {
            await _uiManager.HandleSendFileAsync(_contactManager, _fileTransfer, _messageHandler, _historyManager, _sharedKeys, _myLogin);
        }

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

        private byte[] GetMyPublicKey()
        {
            return _myECDH.PublicKey.ToByteArray();
        }

        private void MainForm_FormClosing(object s, FormClosingEventArgs e)
        {
            _uiManager.StopContactUpdateTimer();
            _cancellationTokenSource?.Cancel();
            try
            {
                _udpManager?.Dispose();
                _tcpServer?.Stop();
                _bufferManager?.SaveBuffer();
                _myECDH?.Clear();
                _myECDH?.Dispose();
                SaveSettings();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error during form closing: {ex.Message}");
            }
            Logger.Log("Application closing");
        }

        private void btnExit_Click(object s, EventArgs e)
        {
            _uiManager.ExitApplication(_bufferManager);
        }

        private void btnSettings_Click(object s, EventArgs e)
        {
            _uiManager.ShowSettingsForm(ref _myLogin, ref _myName, ref _myIP, ref _myStatus, () =>
            {
                _udpManager?.Dispose();
                _tcpServer?.Stop();
                _udpManager = new UdpManager(_myLogin, _myName, _myStatus, GetMyPublicKey, _contactManager.HandleUdpMessage);
                _tcpServer = new TcpServer(_messageHandler.HandleKeyExchange,
                    (sender, encrypted, nonce, tag) => _messageHandler.HandleMessageAsync(sender, encrypted, nonce, tag, _uiManager.SelectedContact, _uiManager.UpdateHistoryDisplay),
                    (sender, fileName, fileSize, nonce, isChunked) => _messageHandler.HandleFileAsync(sender, fileName, fileSize, nonce, isChunked, _uiManager.SelectedContact, _uiManager.UpdateHistoryDisplay));
                Task.Run(() => _udpManager.StartBroadcastAsync(), _cancellationTokenSource.Token);
                Task.Run(() => _udpManager.StartListenerAsync(), _cancellationTokenSource.Token);
                Task.Run(() => _tcpServer.StartAsync(), _cancellationTokenSource.Token);
            });
        }

        private void btnViewLogs_Click(object s, EventArgs e)
        {
            _uiManager.ViewLogs();
        }

        private void btnOpenSettingsFolder_Click(object s, EventArgs e)
        {
            _uiManager.OpenSettingsFolder();
        }

        private void btnDeleteAccount_Click(object s, EventArgs e)
        {
            _uiManager.DeleteAccount();
        }

        private void lstContacts_DrawItem(object s, DrawListViewItemEventArgs e)
        {
            _uiManager.LstContacts_DrawItem(s, e);
        }

        private void lstContacts_SelectedIndexChanged(object s, EventArgs e)
        {
            _uiManager.LstContacts_SelectedIndexChanged(s, e);
        }

        private void txtMessage_KeyDown(object s, KeyEventArgs e)
        {
            _uiManager.TxtMessage_KeyDown(s, e);
        }

        private void cmbStatus_SelectedIndexChanged(object s, EventArgs e)
        {
            _uiManager.CmbStatus_SelectedIndexChanged(s, e, ref _myStatus, SendStatusUpdateBroadcast);
        }
    }
}