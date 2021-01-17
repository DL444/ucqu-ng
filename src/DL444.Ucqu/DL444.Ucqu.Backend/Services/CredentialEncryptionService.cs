using System;
using System.IO;
using System.Security.Cryptography;
using DL444.Ucqu.Models;

namespace DL444.Ucqu.Backend.Services
{
    public interface ICredentialEncryptionService
    {
        void EncryptCredential(StudentCredential credential);
        void DecryptCredential(StudentCredential credential);
    }

    internal class CredentialEncryptionService : ICredentialEncryptionService, IDisposable
    {
        public CredentialEncryptionService(string key)
        {
            aes = Aes.Create();
            aes.Key = Convert.FromBase64String(key);
        }

        public void EncryptCredential(StudentCredential credential)
        {
            if (credential.Iv != null)
            {
                throw new InvalidOperationException("IV is not empty. Possibly already encrypted.");
            }
            aes.GenerateIV();
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

        public void DecryptCredential(StudentCredential credential)
        {
            if (credential.Iv == null)
            {
                throw new ArgumentException("Missing IV in provided credential.");
            }
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
            credential.Iv = null;
        }

        private Aes aes;

        #region IDisposable
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    aes.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
