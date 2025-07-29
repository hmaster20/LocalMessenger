using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;


namespace LocalMessenger.Helpers
{
    internal class Paths
    {
        private static string AppDataPath;
        private static string AttachmentsPath;
        private static string HistoryPath;
        private static string SettingsFile;

        // Вначале выполняется InitializePaths затем InitializeDirectories

        public static void InitPaths()
        {
            AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalMessenger");
            AttachmentsPath = Path.Combine(AppDataPath, "attachments");
            HistoryPath = Path.Combine(AppDataPath, "history");
            SettingsFile = Path.Combine(AppDataPath, "settings.json");
            Logger.Log($"Paths initialized: AppData={AppDataPath}, Settings={SettingsFile}");
        }

        public static void InitDirectories()
        {
            Directory.CreateDirectory(AppDataPath);
            Directory.CreateDirectory(AttachmentsPath);
            Directory.CreateDirectory(HistoryPath);
            Logger.Log("Directories created or verified");
        }

        public static string GetAppDataPath() => AppDataPath;
        public static string GetAttachmentsPath() => AttachmentsPath;
        public static string GetHistoryPath() => HistoryPath;
        public static string GetSettingsFile() => SettingsFile;
    }

    public class MessageBufferManager
    {
        private readonly string bufferPath;
        private List<BufferedMessage> buffer;

        public MessageBufferManager(string appDataPath)
        {
            bufferPath = Path.Combine(appDataPath, "message_buffer.json");
            buffer = LoadBuffer();
        }

        public void AddToBuffer(string contactIP, string message)
        {
            buffer.Add(new BufferedMessage
            {
                ContactIP = contactIP,
                Message = message,
                Timestamp = DateTime.Now
            });
            SaveBuffer();
        }

        public List<BufferedMessage> GetBuffer()
        {
            return buffer;
        }

        public void RemoveFromBuffer(BufferedMessage message)
        {
            buffer.Remove(message);
            SaveBuffer();
        }

        public void SaveBuffer()
        {
            var json = JsonConvert.SerializeObject(buffer, Formatting.Indented);
            File.WriteAllText(bufferPath, json);
        }

        private List<BufferedMessage> LoadBuffer()
        {
            if (File.Exists(bufferPath))
            {
                try
                {
                    var json = File.ReadAllText(bufferPath);
                    return JsonConvert.DeserializeObject<List<BufferedMessage>>(json) ?? new List<BufferedMessage>();
                }
                catch
                {
                    return new List<BufferedMessage>();
                }
            }
            return new List<BufferedMessage>();
        }
    }

    public class BufferedMessage
    {
        public string ContactIP { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class HistoryManager
    {
        private readonly string HistoryPath;
        private readonly byte[] encryptionKey;
        private readonly Dictionary<string, List<Message>> messageCache = new Dictionary<string, List<Message>>();

        public HistoryManager(string appDataPath, byte[] encryptionKey)
        {
            this.HistoryPath = Path.Combine(appDataPath, "history");
            this.encryptionKey = encryptionKey;
            Directory.CreateDirectory(HistoryPath);
        }

        /// <summary>
        /// Добавить сообщение в историю
        /// </summary>
        public void AddMessage(string contact, Message message)
        {
            var fileName = Path.Combine(HistoryPath, $"{contact}.json");
            var messages = LoadMessages(contact);
            messages.Add(message);
            SaveMessages(fileName, messages);
            messageCache[contact] = messages; // Обновляем кэш
        }

        /// <summary>
        /// Загрузить историю сообщений для контакта
        /// </summary>
        public List<Message> LoadMessages(string contact)
        {
            if (messageCache.ContainsKey(contact))
            {
                Logger.Log($"Loaded {contact} history from cache");
                return messageCache[contact];
            }

            var fileName = Path.Combine(HistoryPath, $"{contact}.json");
            if (!File.Exists(fileName))
            {
                Logger.Log($"History file not found for contact: {contact} ({fileName})");
                return new List<Message>();
            }

            try
            {
                var encryptedData = File.ReadAllBytes(fileName);
                var decryptedJson = Decrypt(encryptedData, encryptionKey);
                var messages = JsonConvert.DeserializeObject<List<Message>>(decryptedJson) ?? new List<Message>();
                messageCache[contact] = messages; // Сохраняем в кэш
                Logger.Log($"Loaded {messages.Count} messages for contact: {contact}");
                return messages;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading history for {contact}: {ex.Message}");
                return new List<Message>();
            }
        }

        /// <summary>
        /// Сохранить историю сообщений в файл
        /// </summary>
        private void SaveMessages(string fileName, List<Message> messages)
        {
            try
            {
                var json = JsonConvert.SerializeObject(messages, Formatting.Indented);
                var encryptedData = Encrypt(json, encryptionKey);
                if (File.Exists(fileName))
                {
                    var backupFile = $"{fileName}.backup_{DateTime.Now:yyyyMMdd_HHmmss}";
                    File.Copy(fileName, backupFile);
                    Logger.Log($"Created backup of history file: {backupFile}");
                }
                File.WriteAllBytes(fileName, encryptedData);
                Logger.Log($"Saved history to {fileName}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error saving history to {fileName}: {ex.Message}");
                throw; // Или обработать ошибку иным способом
            }
        }

        /// <summary>
        /// Зашифровать данные AES-256-CBC
        /// </summary>
        private byte[] Encrypt(string plainText, byte[] key)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                aes.GenerateIV();
                var iv = aes.IV;

                var plainBytes = Encoding.UTF8.GetBytes(plainText);

                using (var encryptor = aes.CreateEncryptor())
                {
                    var cipherText = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                    var result = new byte[iv.Length + cipherText.Length];
                    Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                    Buffer.BlockCopy(cipherText, 0, result, iv.Length, cipherText.Length);

                    return result;
                }
            }
        }

        /// <summary>
        /// Расшифровать данные AES-256-CBC
        /// </summary>
        private string Decrypt(byte[] cipherData, byte[] key)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                var iv = new byte[16];
                var cipherText = new byte[cipherData.Length - iv.Length];

                Buffer.BlockCopy(cipherData, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(cipherData, iv.Length, cipherText, 0, cipherText.Length);

                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                {
                    var decrypted = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                    return Encoding.UTF8.GetString(decrypted);
                }
            }
        }
    }

    public class Message
    {
        public string Sender { get; set; }
        public string Content { get; set; }
        public MessageType Type { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public static class KeyManager
    {
        private static readonly string KeyFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LocalMessenger", "key.bin");

        public static byte[] GenerateAndSaveKey(string password)
        {
            try
            {
                var key = new byte[32]; // 256-bit key for AES
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(key);
                }

                // Шифруем ключ с использованием пароля
                using (var aes = Aes.Create())
                {
                    aes.Key = DeriveKeyFromPassword(password);
                    aes.GenerateIV();
                    var iv = aes.IV;

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        var encryptedKey = encryptor.TransformFinalBlock(key, 0, key.Length);
                        var result = new byte[iv.Length + encryptedKey.Length];
                        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                        Buffer.BlockCopy(encryptedKey, 0, result, iv.Length, encryptedKey.Length);
                        File.WriteAllBytes(KeyFile, result);
                    }
                }

                Logger.Log("Encryption key generated and saved");
                return key;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error generating and saving key: {ex.Message}");
                throw;
            }
        }

        public static byte[] LoadKey(string password)
        {
            try
            {
                if (!File.Exists(KeyFile))
                {
                    Logger.Log("Encryption key file not found");
                    throw new FileNotFoundException("Encryption key file not found");
                }

                var data = File.ReadAllBytes(KeyFile);
                var iv = new byte[16];
                var encryptedKey = new byte[data.Length - iv.Length];
                Buffer.BlockCopy(data, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(data, iv.Length, encryptedKey, 0, encryptedKey.Length);

                using (var aes = Aes.Create())
                {
                    aes.Key = DeriveKeyFromPassword(password);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        var key = decryptor.TransformFinalBlock(encryptedKey, 0, encryptedKey.Length);
                        Logger.Log("Encryption key loaded successfully");
                        return key;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading encryption key: {ex.Message}");
                throw;
            }
        }

        private static byte[] DeriveKeyFromPassword(string password)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("LocalMessengerSalt"), 10000))
            {
                return deriveBytes.GetBytes(32); // 256-bit key
            }
        }
    }

    public static class SecurityHelper
    {
        public static byte[] GenerateNonce()
        {
            var nonce = new byte[16]; // 128-bit nonce for AES
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(nonce);
            }
            return nonce;
        }

        public static byte[] DeriveSharedKey(ECDiffieHellmanCng myECDH, byte[] contactPublicKey)
        {
            return myECDH.DeriveKeyMaterial(CngKey.Import(contactPublicKey, CngKeyBlobFormat.EccPublicBlob));
        }

        public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        public static byte[] Decrypt(byte[] cipherText, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                }
            }
        }
    }

    public class FileTransfer
    {
        private readonly string AttachmentsPath;
        private readonly byte[] encryptionKey;

        public FileTransfer(byte[] key)
        {
            encryptionKey = key;
            AttachmentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalMessenger", "attachments");
            Directory.CreateDirectory(AttachmentsPath);
        }

        public async Task SendFile(string filePath, string targetIP)
        {
            var fileName = Path.GetFileName(filePath);
            var fileSize = new FileInfo(filePath).Length;
            var nonce = SecurityHelper.GenerateNonce();

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
                            var encrypted = Encrypt(buffer, bytesRead, encryptionKey, nonce);
                            await stream.WriteAsync(encrypted, 0, encrypted.Length);
                            totalSent += bytesRead;
                        }
                    }
                }
            }
        }

        private byte[] Encrypt(byte[] data, int length, byte[] key, byte[] nonce)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = nonce;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(data, 0, length);
                }
            }
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

                    var filePath = Path.Combine(AttachmentsPath, fileName);
                    using (var fileStream = File.Create(filePath))
                    {
                        var remaining = fileSize;
                        var fileBuffer = new byte[1024 * 1024]; // Переименовали переменную

                        while (remaining > 0)
                        {
                            bytesRead = await stream.ReadAsync(fileBuffer, 0, (int)Math.Min(fileBuffer.Length, remaining));
                            var decrypted = Decrypt(fileBuffer, bytesRead, encryptionKey, nonce);
                            await fileStream.WriteAsync(decrypted, 0, decrypted.Length);
                            remaining -= bytesRead;
                        }
                    }
                }
            }
        }

        private byte[] Decrypt(byte[] cipherText, int length, byte[] key, byte[] nonce)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = nonce;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(cipherText, 0, length);
                }
            }
        }






    }

    //public static class SecurityHelper
    //{
    //    public static byte[] GenerateNonce()
    //    {
    //        var nonce = new byte[16]; // 128-bit nonce for AES
    //        using (var rng = RandomNumberGenerator.Create())
    //        {
    //            rng.GetBytes(nonce);
    //        }
    //        return nonce;
    //    }
    //}

    /// <summary>
    /// Статусы пользователя
    /// </summary>
    public enum UserStatus
    {
        Online,
        Busy,
        Away,
        DoNotDisturb
    }

    /// <summary>
    /// Типы сообщений
    /// </summary>
    public enum MessageType
    {
        Text,
        File,
        Image,
        Emoji,
        GroupMessage,
        StatusUpdate,
        KeyExchange
    }

    /// <summary>
    /// Режимы тем оформления
    /// </summary>
    public enum ThemeMode
    {
        Light,
        Dark
    }
}