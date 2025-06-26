using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using LocalMessenger.Core.Models;
using Newtonsoft.Json;

namespace LocalMessenger.Core.Services
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
            var json = JsonConvert.SerializeObject(messages, Formatting.Indented);
            var encryptedData = Encrypt(json, encryptionKey);
            File.WriteAllBytes(fileName, encryptedData);
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


}