﻿using System;
using System.IO;
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
            var nonce = new byte[16]; // 128-bit nonce for AES
            using (var rng = RandomNumberGenerator.Create())
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

        public static byte[] DeriveSharedKey(ECDiffieHellmanCng myECDH, byte[] contactPublicKey)
        {
            return myECDH.DeriveKeyMaterial(CngKey.Import(contactPublicKey, CngKeyBlobFormat.EccPublicBlob));
        }

        public static byte[] EncryptChunk(byte[] data, int length, byte[] key, byte[] nonce)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = nonce;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }

        public static byte[] DecryptChunk(byte[] cipherText, byte[] key, byte[] nonce)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = nonce;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(cipherText, 0, cipherText.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
        }
    }
}

#region MainForm

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