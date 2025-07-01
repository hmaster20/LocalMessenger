using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using LocalMessenger.Core.Models;
using LocalMessenger.Core.Security;
using LocalMessenger.Utilities;
using Newtonsoft.Json;
using LocalMessenger.Core.Models;

namespace LocalMessenger.Core.Services
{
    public class HistoryManager
    {
        private readonly string _historyPath;
        private readonly byte[] _encryptionKey;
        private readonly Dictionary<string, List<Models.Message>> _messageCache;

        public HistoryManager(string appDataPath, byte[] encryptionKey)
        {
            _historyPath = Path.Combine(appDataPath, "history");
            Directory.CreateDirectory(_historyPath);
            _encryptionKey = encryptionKey;
            _messageCache = new Dictionary<string, List<Models.Message>>();
            Logger.Log("HistoryManager initialized");
        }

        public List<Models.Message> LoadMessages(string contact)
        {
            try
            {
                var fileName = Path.Combine(_historyPath, $"{contact}.json");
                if (!File.Exists(fileName))
                {
                    Logger.Log($"History file not found for {contact}: {fileName}");
                    return new List<Models.Message>();
                }

                var encryptedData = File.ReadAllBytes(fileName);
                if (encryptedData.Length < 16) // Минимальная длина: 16 байт для nonce
                {
                    Logger.Log($"Invalid history file for {contact}: File too small ({encryptedData.Length} bytes)");
                    PromptDeleteCorruptedFile(fileName, contact);
                    return new List<Models.Message>();
                }

                var nonce = encryptedData.Take(16).ToArray();
                var cipherText = encryptedData.Skip(16).ToArray();
                if (cipherText.Length == 0)
                {
                    Logger.Log($"Invalid history file for {contact}: No cipher text after nonce");
                    PromptDeleteCorruptedFile(fileName, contact);
                    return new List<Models.Message>();
                }

                var decryptedJson = CryptoUtils.Decrypt(cipherText, _encryptionKey, nonce);
                var messages = JsonConvert.DeserializeObject<List<Models.Message>>(decryptedJson);
                if (messages == null)
                {
                    Logger.Log($"Invalid history file for {contact}: Deserialized messages are null");
                    PromptDeleteCorruptedFile(fileName, contact);
                    return new List<Models.Message>();
                }

                _messageCache[contact] = messages;
                Logger.Log($"Loaded {messages.Count} messages for {contact}");
                return messages;
            }
            catch (CryptographicException ex)
            {
                Logger.Log($"Cryptographic error loading history for {contact}: {ex.Message}, StackTrace: {ex.StackTrace}");
                PromptDeleteCorruptedFile(Path.Combine(_historyPath, $"{contact}.json"), contact);
                return new List<Models.Message>();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading history for {contact}: {ex.Message}, StackTrace: {ex.StackTrace}");
                return new List<Models.Message>();
            }
        }

        private void PromptDeleteCorruptedFile(string fileName, string contact)
        {
            var result = MessageBox.Show(
                $"History file for contact '{contact}' is corrupted and cannot be loaded.\n" +
                $"Would you like to delete it and start a new history?\nFile: {fileName}",
                "Corrupted History File",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    File.Delete(fileName);
                    _messageCache.Remove(contact);
                    Logger.Log($"Deleted corrupted history file for {contact}: {fileName}");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error deleting corrupted history file for {contact}: {ex.Message}");
                }
            }
        }

        public void SaveMessage(string contact, Models.Message message)
        {
            try
            {
                if (!_messageCache.ContainsKey(contact))
                {
                    _messageCache[contact] = new List<Models.Message>();
                }
                _messageCache[contact].Add(message);

                var json = JsonConvert.SerializeObject(_messageCache[contact], Formatting.None);
                var nonce = CryptoUtils.GenerateNonce();
                var cipherText = CryptoUtils.Encrypt(json, _encryptionKey, nonce);
                var encryptedData = nonce.Concat(cipherText).ToArray();

                var fileName = Path.Combine(_historyPath, $"{contact}.json");
                File.WriteAllBytes(fileName, encryptedData);
                Logger.Log($"Saved message for {contact}: {message.Content}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error saving message for {contact}: {ex.Message}");
            }
        }
    }
}


//{
//    public class HistoryManager
//    {
//        private readonly string HistoryPath;
//        private readonly byte[] encryptionKey;

//        public HistoryManager(string appDataPath, byte[] encryptionKey)
//        {
//            this.HistoryPath = Path.Combine(appDataPath, "history");
//            this.encryptionKey = encryptionKey;
//            Directory.CreateDirectory(HistoryPath);
//        }

//        /// <summary>
//        /// Добавить сообщение в историю
//        /// </summary>
//        public void AddMessage(string contact, Message message)
//        {
//            var fileName = Path.Combine(HistoryPath, $"{contact}.json");
//            var messages = LoadMessages(contact);
//            messages.Add(message);
//            SaveMessages(fileName, messages);
//        }

//        /// <summary>
//        /// Загрузить историю сообщений для контакта
//        /// </summary>
//        public List<Message> LoadMessages(string contact)
//        {
//            var fileName = Path.Combine(HistoryPath, $"{contact}.json");
//            if (!File.Exists(fileName)) return new List<Message>();

//            try
//            {
//                var encryptedData = File.ReadAllBytes(fileName);
//                var decryptedJson = Decrypt(encryptedData, encryptionKey);
//                return JsonConvert.DeserializeObject<List<Message>>(decryptedJson) ?? new List<Message>();
//            }
//            catch (Exception ex)
//            {
//                Logger.Log($"Error loading history for {contact}: {ex.Message}");
//                return new List<Message>();
//            }
//        }

//        /// <summary>
//        /// Сохранить историю сообщений в файл
//        /// </summary>
//        private void SaveMessages(string fileName, List<Message> messages)
//        {
//            var json = JsonConvert.SerializeObject(messages, Formatting.Indented);
//            var encryptedData = Encrypt(json, encryptionKey);
//            File.WriteAllBytes(fileName, encryptedData);
//        }

//        /// <summary>
//        /// Зашифровать данные AES-256-CBC
//        /// </summary>
//        private byte[] Encrypt(string plainText, byte[] key)
//        {
//            using (var aes = Aes.Create())
//            {
//                aes.Key = key;
//                aes.Mode = CipherMode.CBC;
//                aes.Padding = PaddingMode.PKCS7;
                
//                aes.GenerateIV();
//                var iv = aes.IV;

//                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                
//                using (var encryptor = aes.CreateEncryptor())
//                {
//                    var cipherText = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    
//                    var result = new byte[iv.Length + cipherText.Length];
//                    Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
//                    Buffer.BlockCopy(cipherText, 0, result, iv.Length, cipherText.Length);
                    
//                    return result;
//                }
//            }
//        }

//        /// <summary>
//        /// Расшифровать данные AES-256-CBC
//        /// </summary>
//        private string Decrypt(byte[] cipherData, byte[] key)
//        {
//            using (var aes = Aes.Create())
//            {
//                aes.Key = key;
//                aes.Mode = CipherMode.CBC;
//                aes.Padding = PaddingMode.PKCS7;
                
//                var iv = new byte[16];
//                var cipherText = new byte[cipherData.Length - iv.Length];
                
//                Buffer.BlockCopy(cipherData, 0, iv, 0, iv.Length);
//                Buffer.BlockCopy(cipherData, iv.Length, cipherText, 0, cipherText.Length);
                
//                aes.IV = iv;
                
//                using (var decryptor = aes.CreateDecryptor())
//                {
//                    var decrypted = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
//                    return Encoding.UTF8.GetString(decrypted);
//                }
//            }
//        }
//    }
