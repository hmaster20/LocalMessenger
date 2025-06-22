using System;
using System.Security.Cryptography;

namespace LocalMessenger
{
    public static class SecurityHelper
    {
        public static byte[] GenerateKey()
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                return aes.Key;
            }
        }

        public static byte[] Encrypt(byte[] data, byte[] key, byte[] nonce)
        {
            using (var aes = new AesGcm(key))
            {
                var cipherText = new byte[data.Length];
                var tag = new byte[AesGcm.TagByteSizes.MaxSize];
                aes.Encrypt(nonce, data, cipherText, tag);
                return cipherText;
            }
        }

        public static byte[] Decrypt(byte[] cipherText, byte[] key, byte[] nonce, byte[] signature)
        {
            using (var aes = new AesGcm(key))
            {
                var decrypted = new byte[cipherText.Length];
                var tag = new byte[AesGcm.TagByteSizes.MaxSize];
                Buffer.BlockCopy(signature, 0, tag, 0, tag.Length);

                aes.Decrypt(nonce, cipherText, tag, decrypted);
                return decrypted;
            }
        }
    }
}