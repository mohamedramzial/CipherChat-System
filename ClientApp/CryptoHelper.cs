using System;
using System.Security.Cryptography;
using System.Text;

namespace ClientApp // غير هذا الاسم في مشروع العميل إلى ClientApp
{
    public static class CryptoHelper
    {
        // مفتاح التشفير (يجب أن يكون 16 حرفاً بالضبط لـ AES-128)
        private static readonly string Key = "1234567890123456";
        private static readonly string IV = "1234567890123456";

        public static string Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(Key);
                aes.IV = Encoding.UTF8.GetBytes(IV);
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encrypted = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                return Convert.ToBase64String(encrypted);
            }
        }

        public static string Decrypt(string cipherText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(Key);
                aes.IV = Encoding.UTF8.GetBytes(IV);
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] inputBytes = Convert.FromBase64String(cipherText);
                byte[] original = decryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                return Encoding.UTF8.GetString(original);
            }
        }
    }
}