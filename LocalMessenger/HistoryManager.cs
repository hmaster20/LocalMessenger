using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace LocalMessenger
{
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
}