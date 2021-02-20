using System;
using System.IO;
using System.Security.Cryptography;
using DL444.Ucqu.Models;
using Microsoft.Extensions.Configuration;

namespace DL444.Ucqu.Backend.Services
{
    public interface ICredentialEncryptionService
    {
        void EncryptCredential(StudentCredential credential);
        void DecryptCredential(StudentCredential credential);
    }

    internal class CredentialEncryptionService : ICredentialEncryptionService
    {
        public CredentialEncryptionService(IConfiguration config)
        {
            string key = config.GetValue<string>("Credential:EncryptionKey");
            this.key = Convert.FromBase64String(key);
        }

        public void EncryptCredential(StudentCredential credential)
        {
            if (credential.Iv != null)
            {
                throw new InvalidOperationException("IV is not empty. Possibly already encrypted.");
            }
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                credential.Iv = Convert.ToBase64String(aes.IV);
                using (var encryptedStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(encryptedStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (var writer = new StreamWriter(cryptoStream))
                        {
                            writer.Write(credential.PasswordHash);
                        }
                    }
                    credential.PasswordHash = Convert.ToBase64String(encryptedStream.ToArray());
                }
            }
        }

        public void DecryptCredential(StudentCredential credential)
        {
            if (credential.Iv == null)
            {
                throw new ArgumentException("Missing IV in provided credential.");
            }
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = Convert.FromBase64String(credential.Iv);
                using (var encryptedStream = new MemoryStream(Convert.FromBase64String(credential.PasswordHash)))
                {
                    using (var cryptoStream = new CryptoStream(encryptedStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (var reader = new StreamReader(cryptoStream))
                        {
                            credential.PasswordHash = reader.ReadToEnd();
                        }
                    }
                }
            }
            credential.Iv = null;
        }

        private byte[] key;
    }
}
