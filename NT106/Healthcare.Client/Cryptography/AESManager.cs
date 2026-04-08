using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Healthcare.Client.Cryptography
{
    public static class AESManager
    {
        
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("Connguoilauocmocualoaicaheo");

        public static string Encrypt(string plainText, string password)
        {
            try
            {
                using var aes = Aes.Create();
                using var keyDerivation = new Rfc2898DeriveBytes(password, Salt, 100000, HashAlgorithmName.SHA256);

                aes.Key = keyDerivation.GetBytes(32); // 256 bit
                aes.IV = keyDerivation.GetBytes(16);  // 128 bit

                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
            catch { return null; }
        }
        public static string Decrypt(string cipherText, string password)
        {
            try
            {
                using var aes = Aes.Create();
                using var keyDerivation = new Rfc2898DeriveBytes(password, Salt, 100000, HashAlgorithmName.SHA256);

                aes.Key = keyDerivation.GetBytes(32);
                aes.IV = keyDerivation.GetBytes(16);

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);

                return sr.ReadToEnd();
            }
            catch { return "[Lỗi Giải Mã: Sai Mật Khẩu]"; }
        }
    }
}