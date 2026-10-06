using System;
using System.Security.Cryptography;
using System.Text;
namespace AcademicParaphraser.Infrastructure.Persistence
{
    public sealed class WindowsTextProtector : ITextProtector
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("AkademikParafraz.History.1");
        public byte[] Protect(string text) => ProtectedData.Protect(Encoding.UTF8.GetBytes(text), Entropy, DataProtectionScope.CurrentUser);
        public string Unprotect(byte[] blob) => Encoding.UTF8.GetString(ProtectedData.Unprotect(blob, Entropy, DataProtectionScope.CurrentUser));
    }
    public sealed class AesTextProtector : ITextProtector
    {
        private readonly byte[] key; public AesTextProtector(byte[] key)
        {
            if (key.Length != 32)
                throw new ArgumentException("Anahtar 32 byte olmalı.");
            this.key = (byte[])key.Clone();
        }
        public byte[] Protect(string text)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                byte[] raw = Encoding.UTF8.GetBytes(text), encrypted;
                using (var transform = aes.CreateEncryptor())
                    encrypted = transform.TransformFinalBlock(raw, 0, raw.Length);
                var body = new byte[16 + encrypted.Length];
                Buffer.BlockCopy(aes.IV, 0, body, 0, 16);
                Buffer.BlockCopy(encrypted, 0, body, 16, encrypted.Length);
                using (var mac = new HMACSHA256(key))
                {
                    var tag = mac.ComputeHash(body);
                    var output = new byte[body.Length + 32];
                    Buffer.BlockCopy(body, 0, output, 0, body.Length);
                    Buffer.BlockCopy(tag, 0, output, body.Length, 32);
                    return output;
                }
            }
        }
        public string Unprotect(byte[] blob)
        {
            if (blob.Length < 64)
                throw new CryptographicException("Geçmiş kaydı geçersiz.");
            int length = blob.Length - 32;
            using (var mac = new HMACSHA256(key))
            {
                var tag = mac.ComputeHash(blob, 0, length);
                int delta = 0;
                for (int i = 0; i < 32; i++)
                    delta |= tag[i] ^ blob[length + i];
                if (delta != 0)
                    throw new CryptographicException("Geçmiş kaydı doğrulanamadı.");
            }
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                var iv = new byte[16];
                Buffer.BlockCopy(blob, 0, iv, 0, 16);
                aes.IV = iv;
                using (var transform = aes.CreateDecryptor())
                    return Encoding.UTF8.GetString(transform.TransformFinalBlock(blob, 16, length - 16));
            }
        }
    }
}
