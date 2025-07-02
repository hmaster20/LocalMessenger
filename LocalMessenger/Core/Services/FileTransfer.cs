using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LocalMessenger.Core.Security;
using LocalMessenger.Utilities;
using LocalMessenger.UI.Forms;

namespace LocalMessenger.Core.Services
{
    public class FileTransfer
    {
        private readonly string _attachmentsPath;
        private readonly byte[] _encryptionKey;

        public FileTransfer(byte[] key)
        {
            _encryptionKey = key;
            _attachmentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalMessenger", "attachments");
            Directory.CreateDirectory(_attachmentsPath);
        }

        public async Task SendFile(string filePath, string targetIP)
        {
            var fileName = Path.GetFileName(filePath);
            var fileSize = new FileInfo(filePath).Length;
            var nonce = CryptoUtils.GenerateNonce();

            using (var client = new TcpClient())
            {
                await client.ConnectAsync(targetIP, 12000);
                using (var stream = client.GetStream())
                {
                    var header = $"FILE|{fileName}|{fileSize}|{Convert.ToBase64String(nonce)}";
                    var headerBytes = Encoding.UTF8.GetBytes(header);
                    await stream.WriteAsync(headerBytes, 0, headerBytes.Length);

                    using (var fileStream = File.OpenRead(filePath))
                    {
                        var buffer = new byte[1024 * 1024]; // 1 MB chunks
                        int bytesRead;
                        long totalSent = 0;

                        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            var encrypted = CryptoUtils.EncryptChunk(buffer, bytesRead, _encryptionKey, nonce);
                            await stream.WriteAsync(encrypted, 0, encrypted.Length);
                            totalSent += bytesRead;
                        }
                    }
                }
            }
            Logger.Log($"File sent successfully to {targetIP}: {filePath}");
        }

        public async Task ReceiveFile(TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();
                var headerBuffer = new byte[4096];
                var bytesRead = await stream.ReadAsync(headerBuffer, 0, headerBuffer.Length);
                var header = Encoding.UTF8.GetString(headerBuffer, 0, bytesRead);
                var parts = header.Split('|');

                if (parts[0] == "FILE")
                {
                    var fileName = parts[1];
                    var fileSize = Convert.ToInt64(parts[2]);
                    var nonce = Convert.FromBase64String(parts[3]);

                    var filePath = GenerateUniqueFilePath(fileName);
                    using (var fileStream = File.Create(filePath))
                    {
                        var remaining = fileSize;
                        var fileBuffer = new byte[1024 * 1024];

                        while (remaining > 0)
                        {
                            bytesRead = await stream.ReadAsync(fileBuffer, 0, (int)Math.Min(fileBuffer.Length, remaining));
                            var decrypted = CryptoUtils.DecryptChunk(fileBuffer.Take(bytesRead).ToArray(), _encryptionKey, nonce);
                            await fileStream.WriteAsync(decrypted, 0, decrypted.Length);
                            remaining -= bytesRead;
                        }
                    }
                    Logger.Log($"File received successfully: {filePath}");
                }
            }
        }

        public async Task<bool> SendLargeFileAsync(string contactIP, string messagePrefix, string fileName, long fileSize, string filePath, byte[] sharedKey, string myLogin)
        {
            const int chunkSize = 1024 * 1024; // 1 MB chunks
            using (var client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync(contactIP, 12000);
                    using (var stream = client.GetStream())
                    {
                        var message = $"{messagePrefix}_CHUNKED|{myLogin}|{fileName}|{fileSize}|{chunkSize}";
                        var messageBytes = Encoding.UTF8.GetBytes(message);
                        await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                        await stream.FlushAsync();

                        var progressForm = new ProgressForm(fileName, fileSize);
                        progressForm.Show();

                        using (var fs = File.OpenRead(filePath))
                        {
                            var buffer = new byte[chunkSize];
                            long totalSent = 0;
                            int bytesRead;
                            while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                var nonce = CryptoUtils.GenerateNonce();
                                var encryptedChunk = CryptoUtils.EncryptChunk(buffer, bytesRead, sharedKey, nonce);
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

        public string GenerateUniqueFilePath(string fileName)
        {
            var filePath = Path.Combine(_attachmentsPath, $"{Guid.NewGuid()}_{fileName}");
            Logger.Log($"Generated unique file path: {filePath}");
            return filePath;
        }
    }
}


#region MainForm


//public async Task<bool> SendFileAsync(string contactIP, string message, string filePath)
//        {
//            using (var client = new TcpClient())
//            {
//                try
//                {
//                    await client.ConnectAsync(contactIP, 12000);
//                    using (var stream = client.GetStream())
//                    {
//                        var messageBytes = Encoding.UTF8.GetBytes(message);
//                        await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
//                        await stream.FlushAsync();

//                        using (var fs = File.OpenRead(filePath))
//                        {
//                            var buffer = new byte[4096];
//                            int bytesRead;
//                            while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
//                            {
//                                await stream.WriteAsync(buffer, 0, bytesRead);
//                            }
//                        }
//                    }
//                    Logger.Log($"File sent successfully to {contactIP}: {filePath}");
//                    return true;
//                }
//                catch (Exception ex)
//                {
//                    Logger.Log($"File send error to {contactIP}: {ex.Message}");
//                    return false;
//                }
//            }
//        }

//public async Task<bool> SendLargeFileAsync(string contactIP, string messagePrefix, string fileName, long fileSize, string filePath, byte[] sharedKey)
//        {
//            const int chunkSize = 1024 * 1024; // 1 MB chunks
//            using (var client = new TcpClient())
//            {
//                try
//                {
//                    await client.ConnectAsync(contactIP, 12000);
//                    using (var stream = client.GetStream())
//                    {
//                        // Send file metadata
//                        var message = $"{messagePrefix}_CHUNKED|{myLogin}|{fileName}|{fileSize}|{chunkSize}";
//                        var messageBytes = Encoding.UTF8.GetBytes(message);
//                        await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
//                        await stream.FlushAsync();

//                        // Show progress form
//                        var progressForm = new ProgressForm(fileName, fileSize);
//                        progressForm.Show();

//                        using (var fs = File.OpenRead(filePath))
//                        {
//                            var buffer = new byte[chunkSize];
//                            long totalSent = 0;
//                            int bytesRead;
//                            while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
//                            {
//                                // Encrypt chunk
//                                var nonce = GenerateNonce();
//                                var encryptedChunk = EncryptChunk(buffer, bytesRead, sharedKey, nonce);
//                                var chunkMessage = $"{Convert.ToBase64String(encryptedChunk)}|{Convert.ToBase64String(nonce)}|{bytesRead}";
//                                var chunkBytes = Encoding.UTF8.GetBytes(chunkMessage);
//                                await stream.WriteAsync(chunkBytes, 0, chunkBytes.Length);
//                                await stream.FlushAsync();

//                                totalSent += bytesRead;
//                                progressForm.UpdateProgress(totalSent);
//                            }
//                        }

//                        progressForm.Close();
//                        Logger.Log($"Large file sent successfully to {contactIP}: {filePath}");
//                        return true;
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Logger.Log($"Large file send error: {ex.Message}");
//                    return false;
//                }
//            }
//        }



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

#endregion