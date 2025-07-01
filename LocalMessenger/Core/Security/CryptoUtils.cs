using System;
using System.Security.Cryptography;
using System.Text;

namespace LocalMessenger.Core.Security
{
    public static class CryptoUtils
    {
            public static byte[] GenerateEncryptionKey()
            {
                using (var aes = Aes.Create())
                {
                    aes.GenerateKey();
                    return aes.Key;
                }
            }

            public static byte[] GenerateNonce()
            {
                var nonce = new byte[16];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(nonce);
                }
                return nonce;
            }

            public static byte[] Encrypt(string plainText, byte[] key, byte[] nonce)
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = nonce;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        var plainBytes = Encoding.UTF8.GetBytes(plainText);
                        return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    }
                }
            }

            public static string Decrypt(byte[] cipherText, byte[] key, byte[] nonce)
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = nonce;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        var decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }

            #region FileTransfer
            //private byte[] Encrypt(byte[] data, int length, byte[] key, byte[] nonce)
            //{
            //    using (Aes aes = Aes.Create())
            //    {
            //        aes.Key = key;
            //        aes.IV = nonce;
            //        aes.Mode = CipherMode.CBC;
            //        aes.Padding = PaddingMode.PKCS7;

            //        using (var encryptor = aes.CreateEncryptor())
            //        {
            //            return encryptor.TransformFinalBlock(data, 0, length);
            //        }
            //    }
            //}
            //private byte[] Decrypt(byte[] cipherText, int length, byte[] key, byte[] nonce)
            //{
            //    using (Aes aes = Aes.Create())
            //    {
            //        aes.Key = key;
            //        aes.IV = nonce;
            //        aes.Mode = CipherMode.CBC;
            //        aes.Padding = PaddingMode.PKCS7;

            //        using (var decryptor = aes.CreateDecryptor())
            //        {
            //            return decryptor.TransformFinalBlock(cipherText, 0, length);
            //        }
            //    }
            //}

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
            #endregion

            #region SecurityHelper
            //public static byte[] GenerateNonce()
            //{
            //    var nonce = new byte[16]; // 128-bit nonce for AES
            //    using (var rng = new RNGCryptoServiceProvider())
            //    {
            //        rng.GetBytes(nonce);
            //    }
            //    return nonce;
            //}

            #endregion

            #region MainForm

            //private byte[] GenerateNonce()
            //{
            //    var nonce = new byte[16];
            //    using (var rng = new RNGCryptoServiceProvider())
            //    {
            //        rng.GetBytes(nonce);
            //    }
            //    return nonce;
            //}

            //private string Decrypt(byte[] cipherText, byte[] key, byte[] nonce, byte[] tag)
            //{
            //    Logger.Log($"Decrypting cipher text length: {cipherText.Length}");
            //    using (Aes aes = Aes.Create())
            //    {
            //        aes.Key = key;
            //        aes.IV = nonce;
            //        aes.Mode = CipherMode.CBC;
            //        aes.Padding = PaddingMode.PKCS7;

            //        using (var decryptor = aes.CreateDecryptor())
            //        {
            //            var decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
            //            var result = Encoding.UTF8.GetString(decryptedBytes);
            //            Logger.Log($"Decrypted text: {result}");
            //            return result;
            //        }
            //    }
            //}

            //private byte[] Encrypt(string plainText, byte[] key, byte[] nonce)
            //{
            //    Logger.Log($"Encrypting text: {plainText}");
            //    using (Aes aes = Aes.Create())
            //    {
            //        aes.Key = key;
            //        aes.IV = nonce;
            //        aes.Mode = CipherMode.CBC;
            //        aes.Padding = PaddingMode.PKCS7;

            //        using (var encryptor = aes.CreateEncryptor())
            //        {
            //            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            //            var result = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            //            Logger.Log($"Encrypted text length: {result.Length}");
            //            return result;
            //        }
            //    }
            //}

            #endregion
        }
}