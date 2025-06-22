using System;
using System.Security.Cryptography;

namespace LocalMessenger
{
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
    }
}