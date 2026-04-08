using System;
using System.Security.Cryptography;
using System.Text;

namespace Healthcare.Client.Cryptography
{
    public class RSAManager
    {
        public class RsaKeyPair
        {
            public string PublicKey { get; set; }  // Khóa công khai (Lưu lên Supabase)
            public string PrivateKey { get; set; } // Khóa bí mật
        }

        // 1. Tạo cặp khóa mới
        public static RsaKeyPair GenerateKeys()
        {
            using RSA rsa = RSA.Create(2048);
            return new RsaKeyPair
            {
                PublicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey()),
                PrivateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey())
            };
        }

        // 2. Mã hóa tin nhắn 
        public static string Encrypt(string plainText, string receiverPublicKeyBase64)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            byte[] dataBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] keyBytes = Convert.FromBase64String(receiverPublicKeyBase64);

            using RSA rsa = RSA.Create();
            rsa.ImportRSAPublicKey(keyBytes, out _);

            byte[] encryptedBytes = rsa.Encrypt(dataBytes, RSAEncryptionPadding.OaepSHA256);
            return Convert.ToBase64String(encryptedBytes);
        }

        // 3. Giải mã tin nhắn 
        public static string Decrypt(string cipherTextBase64, string myPrivateKeyBase64)
        {
            if (string.IsNullOrEmpty(cipherTextBase64)) return cipherTextBase64;

            byte[] encryptedBytes = Convert.FromBase64String(cipherTextBase64);
            byte[] keyBytes = Convert.FromBase64String(myPrivateKeyBase64);

            using RSA rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(keyBytes, out _);

            byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}