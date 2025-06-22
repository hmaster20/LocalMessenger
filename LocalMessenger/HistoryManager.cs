using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace LocalMessenger
{
    public class HistoryManager
    {
        private readonly string HistoryPath;
        private readonly byte[] encryptionKey;

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
        }

        /// <summary>
        /// Загрузить историю сообщений для контакта
        /// </summary>
        public List<Message> LoadMessages(string contact)
        {
            var fileName = Path.Combine(HistoryPath, $"{contact}.json");
            if (!File.Exists(fileName)) return new List<Message>();

            try
            {
                var encryptedData = File.ReadAllBytes(fileName);
                var decryptedJson = Decrypt(encryptedData, encryptionKey);
                return JsonConvert.DeserializeObject<List<Message>>(decryptedJson) ?? new List<Message>();
            }
            catch
            {
                return new List<Message>(); // Ошибка при чтении или расшифровке
            }
        }

        /// <summary>
        /// Сохранить историю сообщений в файл
        /// </summary>
        private void SaveMessages(string fileName, List<Message> messages)
        {
            var json = JsonConvert.SerializeObject(messages, Formatting.Indented);
            var encryptedData = Encrypt(json, encryptionKey);
            File.WriteAllBytes(fileName, encryptedData);
        }

        /// <summary>
        /// Зашифровать данные AES-256-GCM
        /// </summary>
        private byte[] Encrypt(string plainText, byte[] key)
        {
            using (var aes = new AesGcm(key))
            {
                var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
                RandomNumberGenerator.Fill(nonce);

                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var cipherText = new byte[plainBytes.Length];
                var tag = new byte[AesGcm.TagByteSizes.MaxSize];

                aes.Encrypt(nonce, plainBytes, cipherText, tag);

                // Сохранить nonce и tag в начале файла
                var result = new byte[nonce.Length + cipherText.Length + tag.Length];
                Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
                Buffer.BlockCopy(cipherText, 0, result, nonce.Length, cipherText.Length);
                Buffer.BlockCopy(tag, 0, result, nonce.Length + cipherText.Length, tag.Length);

                return result;
            }
        }

        /// <summary>
        /// Расшифровать данные AES-256-GCM
        /// </summary>
        private string Decrypt(byte[] cipherData, byte[] key)
        {
            using (var aes = new AesGcm(key))
            {
                var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
                var tag = new byte[AesGcm.TagByteSizes.MaxSize];
                var cipherText = new byte[cipherData.Length - nonce.Length - tag.Length];

                Buffer.BlockCopy(cipherData, 0, nonce, 0, nonce.Length);
                Buffer.BlockCopy(cipherData, nonce.Length, cipherText, 0, cipherText.Length);
                Buffer.BlockCopy(cipherData, nonce.Length + cipherText.Length, tag, 0, tag.Length);

                var decrypted = new byte[cipherText.Length];
                aes.Decrypt(nonce, cipherText, tag, decrypted);
                return Encoding.UTF8.GetString(decrypted);
            }
        }
    }

    /// <summary>
    /// Структура сообщения
    /// </summary>
    public class Message
    {
        public string Sender { get; set; }
        public string Content { get; set; }
        public MessageType Type { get; set; }
        public DateTime Timestamp { get; } = DateTime.Now;
    }
}