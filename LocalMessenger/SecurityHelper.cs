using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LocalMessenger
{
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
}