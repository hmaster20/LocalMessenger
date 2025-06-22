using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace LocalMessenger
{
    public class FileTransfer
    {
        private readonly string AttachmentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalMessenger", "attachments");
        private readonly byte[] encryptionKey;

        public FileTransfer(byte[] key)
        {
            encryptionKey = key;
            Directory.CreateDirectory(AttachmentsPath);
        }

        public async Task SendFile(string filePath, string targetIP)
        {
            var fileName = Path.GetFileName(filePath);
            var fileSize = new FileInfo(filePath).Length;
            var nonce = GenerateNonce();

            using (var client = new TcpClient())
            {
                await client.ConnectAsync(targetIP, 12000);
                using (var stream = client.GetStream())
                {
                    // Отправить заголовок
                    var header = $"FILE|{fileName}|{fileSize}|{Convert.ToBase64String(nonce)}";
                    var headerBytes = Encoding.UTF8.GetBytes(header);
                    await stream.WriteAsync(headerBytes, 0, headerBytes.Length);

                    // Отправить файл
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        var buffer = new byte[1024 * 1024]; // 1 MB chunks
                        int bytesRead;
                        long totalSent = 0;

                        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            var encrypted = SecurityHelper.Encrypt(buffer, encryptionKey, nonce);
                            await stream.WriteAsync(encrypted, 0, encrypted.Length);
                            totalSent += bytesRead;
                            // Обновить прогресс
                        }
                    }
                }
            }
        }

        public async Task ReceiveFile(TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();
                var buffer = new byte[4096];
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                var header = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var parts = header.Split('|');

                if (parts[0] == "FILE")
                {
                    var fileName = parts[1];
                    var fileSize = Convert.ToInt64(parts[2]);
                    var nonce = Convert.FromBase64String(parts[3]);

                    var filePath = Path.Combine(AttachmentsPath, fileName);
                    using (var fileStream = File.Create(filePath))
                    {
                        var remaining = fileSize;
                        var buffer = new byte[1024 * 1024]; // 1 MB chunks

                        while (remaining > 0)
                        {
                            bytesRead = await stream.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                            var decrypted = SecurityHelper.Decrypt(buffer, encryptionKey, nonce, new byte[AesGcm.TagByteSizes.MaxSize]);
                            await fileStream.WriteAsync(decrypted, 0, bytesRead);
                            remaining -= bytesRead;
                            // Обновить прогресс
                        }
                    }
                }
            }
        }

        private byte[] GenerateNonce()
        {
            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
            RandomNumberGenerator.Fill(nonce);
            return nonce;
        }
    }
}