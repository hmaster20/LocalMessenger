using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LocalMessenger.Core;
using LocalMessenger.Core.Models;
using LocalMessenger.Core.Security;
using LocalMessenger.Core.Services;
using LocalMessenger.Utilities;
using System.IO;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LocalMessenger.Core;
using LocalMessenger.Core.Models;
using LocalMessenger.Core.Security;
using LocalMessenger.Core.Services;
using LocalMessenger.Utilities;

namespace LocalMessenger.UI.Forms
{
    public class MessageHandler
    {
        private readonly HistoryManager _historyManager;
        private readonly MessageBufferManager _bufferManager;
        private readonly FileTransfer _fileTransfer;
        private readonly Dictionary<string, byte[]> _sharedKeys;
        private readonly ECDiffieHellmanCng _myECDH;
        private readonly string _myLogin;
        private readonly NotifyIcon _notifyIcon;
        private readonly Form _mainForm;

        public MessageHandler(HistoryManager historyManager, MessageBufferManager bufferManager, FileTransfer fileTransfer,
            Dictionary<string, byte[]> sharedKeys, ECDiffieHellmanCng myECDH, string myLogin, NotifyIcon notifyIcon, Form mainForm)
        {
            _historyManager = historyManager;
            _bufferManager = bufferManager;
            _fileTransfer = fileTransfer;
            _sharedKeys = sharedKeys;
            _myECDH = myECDH;
            _myLogin = myLogin;
            _notifyIcon = notifyIcon;
            _mainForm = mainForm;
        }

        public async Task HandleMessageAsync(string sender, byte[] encryptedMessage, byte[] nonce, byte[] tag, string selectedContact, Action<string> updateHistoryDisplay)
        {
            try
            {
                if (!_sharedKeys.ContainsKey(sender))
                {
                    Logger.Log($"No shared key for {sender}, cannot decrypt message.");
                    return;
                }

                var decrypted = CryptoUtils.Decrypt(encryptedMessage, _sharedKeys[sender], nonce);
                _historyManager.SaveMessage(sender, new LocalMessenger.Core.Models.Message
                {
                    Sender = sender,
                    Content = decrypted,
                    Type = MessageType.Text,
                    Timestamp = DateTime.Now
                });
                if (selectedContact == sender)
                {
                    updateHistoryDisplay(sender);
                }
                else
                {
                    FlashTaskbar();
                    if (!_mainForm.Visible || _mainForm.WindowState == FormWindowState.Minimized)
                    {
                        _notifyIcon.ShowBalloonTip(3000, "New Message", $"New message from {sender}", ToolTipIcon.Info);
                        Logger.Log($"Showed balloon tip for new message from {sender}");
                    }
                }
                Logger.Log($"Received message from {sender}: {decrypted}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error handling message from {sender}: {ex.Message}");
            }
        }

        public async Task HandleFileAsync(string sender, string fileName, long fileSize, string nonceBase64, bool isChunked, string selectedContact, Action<string> updateHistoryDisplay)
        {
            try
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
                if (selectedContact == sender)
                {
                    updateHistoryDisplay(sender);
                }
                else
                {
                    FlashTaskbar();
                    if (!_mainForm.Visible || _mainForm.WindowState == FormWindowState.Minimized)
                    {
                        _notifyIcon.ShowBalloonTip(3000, "New File", $"New file from {sender}: {fileName}", ToolTipIcon.Info);
                        Logger.Log($"Showed balloon tip for new file from {sender}: {fileName}");
                    }
                }
                Logger.Log($"Received {messageType} from {sender}: {fileName} saved to {filePath}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error handling file from {sender}: {ex.Message}");
            }
        }

        public void HandleKeyExchange(string sender, byte[] contactPublicKey)
        {
            try
            {
                var sharedKey = CryptoUtils.DeriveSharedKey(_myECDH, contactPublicKey);
                _sharedKeys[sender] = sharedKey;
                Logger.Log($"Shared key established with {sender}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error during key exchange with {sender}: {ex.Message}");
            }
        }

        public async Task<bool> SendMessageAsync(string contactIP, string message)
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
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error sending message to {contactIP}: {ex.Message}");
                return false;
            }
        }

        public async Task SendBufferedMessagesAsync(string contactIP, string contactLogin, Action<string> updateHistoryDisplay)
        {
            var messages = _bufferManager.GetBuffer();
            Logger.Log($"Attempting to send {messages.Count} buffered messages");
            foreach (var msg in messages.ToList())
            {
                try
                {
                    var parts = msg.Message.Split('|');
                    if (parts[0] == "MESSAGE" && parts[1] == contactLogin)
                    {
                        await SendMessageAsync(contactIP ?? msg.ContactIP, msg.Message);
                        _bufferManager.RemoveFromBuffer(msg);
                    }
                    else if ((parts[0].StartsWith("FILE") || parts[0].StartsWith("IMAGE")) && parts[1] == contactLogin)
                    {
                        var fileName = parts[2];
                        var filePath = Path.Combine(Configuration.AttachmentsPath, fileName);
                        if (File.Exists(filePath))
                        {
                            try
                            {
                                await _fileTransfer.SendFileAsync(filePath, contactIP ?? msg.ContactIP);
                                _bufferManager.RemoveFromBuffer(msg);
                                _historyManager.SaveMessage(contactLogin, new LocalMessenger.Core.Models.Message
                                {
                                    Sender = _myLogin,
                                    Content = filePath,
                                    Type = parts[0].StartsWith("IMAGE") ? MessageType.Image : MessageType.File,
                                    Timestamp = DateTime.Now
                                });
                                updateHistoryDisplay(contactLogin);
                            }
                            catch (Exception ex)
                            {
                                Logger.Log($"Error sending file {fileName} to {contactIP ?? msg.ContactIP}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error sending buffered message to {msg.ContactIP}: {ex.Message}");
                }
            }
        }

        private void FlashTaskbar()
        {
            if (!_mainForm.Visible || _mainForm.WindowState == FormWindowState.Minimized)
            {
                NativeMethods.FLASHWINFO flashInfo = new NativeMethods.FLASHWINFO
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.FLASHWINFO)),
                    hwnd = _mainForm.Handle,
                    dwFlags = NativeMethods.FLASHW_ALL,
                    uCount = 3,
                    dwTimeout = 0
                };
                NativeMethods.FlashWindowEx(ref flashInfo);
                Logger.Log("Taskbar flashed for new message");
            }
        }
    }
}