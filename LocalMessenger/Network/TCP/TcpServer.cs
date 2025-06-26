using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LocalMessenger.Core.Security;
using LocalMessenger.Utilities;

namespace LocalMessenger.Network.Tcp
{
    public class TcpServer
    {
        private readonly TcpListener tcpListener;
        private readonly Action<string, byte[]> handleKeyExchange;
        private readonly Action<string, byte[], byte[], byte[]> handleMessage;
        private readonly Action<string, string, long, string, bool> handleFile;

        public TcpServer(Action<string, byte[]> handleKeyExchange,
            Action<string, byte[], byte[], byte[]> handleMessage,
            Action<string, string, long, string, bool> handleFile)
        {
            tcpListener = new TcpListener(IPAddress.Any, 12000);
            this.handleKeyExchange = handleKeyExchange;
            this.handleMessage = handleMessage;
            this.handleFile = handleFile;
        }

        public async Task StartAsync()
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
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                try
                {
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
                        handleKeyExchange(sender, contactPublicKey);
                    }
                    else if (parts[0] == "MESSAGE")
                    {
                        var sender = parts[1];
                        var encryptedMessage = Convert.FromBase64String(parts[2]);
                        var nonce = Convert.FromBase64String(parts[3]);
                        var tag = Convert.FromBase64String(parts[4]);
                        handleMessage(sender, encryptedMessage, nonce, tag);
                    }
                    else if (parts[0].StartsWith("FILE") || parts[0].StartsWith("IMAGE"))
                    {
                        var isChunked = parts[0].EndsWith("_CHUNKED");
                        var sender = parts[1];
                        var fileName = parts[2];
                        var fileSize = long.Parse(parts[3]);
                        var nonce = parts.Length > 3 ? Convert.FromBase64String(parts[3]) : null;
                        handleFile(sender, fileName, fileSize, nonce, isChunked);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error handling client: {ex.Message}");
                }
            }
        }


        #region MainForm


        //private async void StartTcpServer()
        //{
        //    try
        //    {
        //        tcpListener.Start();
        //        Logger.Log("TCP server started on port 12000");
        //        while (true)
        //        {
        //            var client = await tcpListener.AcceptTcpClientAsync();
        //            _ = HandleClientAsync(client);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Log($"Server error: {ex.Message}");
        //        MessageBox.Show($"Server error: {ex.Message}");
        //    }
        //}



        //private async Task HandleClientAsync(TcpClient client)
        //{
        //    using (client)
        //    {
        //        try
        //        {
        //            var remoteEndPoint = client.Client.RemoteEndPoint.ToString();
        //            Logger.Log($"Handling TCP client connection from {remoteEndPoint}");
        //            var stream = client.GetStream();
        //            var buffer = new byte[4096];
        //            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        //            var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        //            Logger.Log($"Received TCP message: {message}");
        //            var parts = message.Split('|');

        //            if (parts[0] == "KEY_EXCHANGE")
        //            {
        //                var sender = parts[1];
        //                var contactPublicKey = Convert.FromBase64String(parts[2]);
        //                contactPublicKeys[sender] = contactPublicKey;

        //                var publicKey = GetMyPublicKey();
        //                var response = $"KEY_EXCHANGE_RESPONSE|{myLogin}|{Convert.ToBase64String(publicKey)}";
        //                var bytes = Encoding.UTF8.GetBytes(response);
        //                await stream.WriteAsync(bytes, 0, bytes.Length);
        //                Logger.Log($"Sent KEY_EXCHANGE_RESPONSE to {sender}");

        //                var sharedKey = myECDH.DeriveKeyMaterial(CngKey.Import(contactPublicKey, CngKeyBlobFormat.EccPublicBlob));
        //                sharedKeys[sender] = sharedKey;
        //                Logger.Log($"Shared key established with {myLogin}");
        //            }
        //            else if (parts[0] == "KEY_EXCHANGE_RESPONSE")
        //            {
        //                var sender = parts[1];
        //                var contactPublicKey = Convert.FromBase64String(parts[2]);
        //                var sharedKey = myECDH.DeriveKeyMaterial(CngKey.Import(contactPublicKey, CngKeyBlobFormat.EccPublicBlob));
        //                sharedKeys[sender] = sharedKey;
        //                Logger.Log($"Shared key for {sender} is set");
        //            }
        //            else if (parts[0] == "MESSAGE")
        //            {
        //                var sender = parts[1];
        //                var encryptedMessage = Convert.FromBase64String(parts[2]);
        //                var nonce = Convert.FromBase64String(parts[3]);
        //                var tag = Convert.FromBase64String(parts[4]);

        //                var decrypted = Decrypt(encryptedMessage, sharedKeys[sender], nonce, tag);
        //                UpdateHistory(sender, decrypted, MessageType.Text, isReceived: true);
        //                if (lstContacts.SelectedItems.Count == 0 || !lstContacts.SelectedItems[0].Text.StartsWith(sender))
        //                {
        //                    blinkingContacts.Add(sender);
        //                }
        //                Logger.Log($"Received MESSAGE from {sender}: {decrypted}");
        //            }
        //            else if (parts[0].StartsWith("FILE") || parts[0].StartsWith("IMAGE"))
        //            {
        //                var isChunked = parts[0].EndsWith("_CHUNKED");
        //                var sender = parts[1];
        //                var fileName = parts[2];
        //                var fileSize = long.Parse(parts[3]);
        //                var filePath = Path.Combine(AttachmentsPath, $"{Guid.NewGuid()}_{fileName}");
        //                var messageType = parts[0].StartsWith("IMAGE") ? MessageType.Image : MessageType.File;

        //                if (!isChunked)
        //                {
        //                    using (var fs = File.Create(filePath))
        //                    {
        //                        var totalRead = 0L;
        //                        while (totalRead < fileSize)
        //                        {
        //                            var toRead = (int)Math.Min(buffer.Length, fileSize - totalRead);
        //                            bytesRead = await stream.ReadAsync(buffer, 0, toRead);
        //                            await fs.WriteAsync(buffer, 0, bytesRead);
        //                            totalRead += bytesRead;
        //                        }
        //                    }
        //                }
        //                else
        //                {
        //                    var chunkSize = int.Parse(parts[4]);
        //                    await ReceiveLargeFileAsync(stream, filePath, fileSize, chunkSize, sharedKeys[sender]);
        //                }

        //                UpdateHistory(sender, filePath, messageType, isReceived: true);
        //                if (lstContacts.SelectedItems.Count == 0 || !lstContacts.SelectedItems[0].Text.StartsWith(sender))
        //                {
        //                    blinkingContacts.Add(sender);
        //                }
        //                Logger.Log($"Received {messageType} from {sender}: {fileName} saved to {filePath}");
        //            }
        //            else if (parts[0] == "GROUP_MESSAGE")
        //            {
        //                var groupID = parts[1];
        //                var sender = parts[5];
        //                var encryptedMessage = Convert.FromBase64String(parts[2]);
        //                var nonce = Convert.FromBase64String(parts[3]);
        //                var tag = Convert.FromBase64String(parts[4]);

        //                var decrypted = Decrypt(encryptedMessage, groupKeys[groupID], nonce, tag);
        //                UpdateGroupHistory(groupID, decrypted, isReceived: true);
        //                Logger.Log($"Received GROUP_MESSAGE for {groupID} from {sender}: {decrypted}");
        //            }
        //            else if (parts[0] == "GROUP_KEY")
        //            {
        //                var groupID = parts[1];
        //                var sender = parts[5];
        //                var encryptedGroupKey = Convert.FromBase64String(parts[2]);
        //                var nonce = Convert.FromBase64String(parts[3]);
        //                var tag = Convert.FromBase64String(parts[4]);

        //                if (sharedKeys.ContainsKey(sender))
        //                {
        //                    var decryptedGroupKeyString = Decrypt(encryptedGroupKey, sharedKeys[sender], nonce, tag);
        //                    groupKeys[groupID] = Convert.FromBase64String(decryptedGroupKeyString);
        //                    Logger.Log($"Received GROUP_KEY for {groupID} from {sender}");
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Log($"Error handling client {client.Client.RemoteEndPoint}: {ex.Message}");
        //        }
        //    }
        //}


        #endregion
    }
}