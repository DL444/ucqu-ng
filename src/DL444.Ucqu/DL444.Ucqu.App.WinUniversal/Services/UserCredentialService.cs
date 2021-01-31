using System;
using System.Linq;
using Windows.Security.Credentials;

namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal class UserCredentialService : ICredentialService
    {
        public string Username
        {
            get
            {
                try
                {
                    PasswordCredential credential = vault.FindAllByResource("User").FirstOrDefault();
                    return credential?.UserName;
                }
                catch (Exception ex) when (ex.HResult == -2147023728)
                {
                    return null;
                }
            }
        }
        public string PasswordHash
        {
            get
            {
                string username = Username;
                return username == null ? null : vault.Retrieve("User", username).Password;
            }
        }

        public string Token { get; set; }
        public bool IsSignedIn => Token != null;

        public void ClearCredential()
        {
            Token = null;
            try
            {
                foreach (PasswordCredential credential in vault.FindAllByResource("User"))
                {
                    vault.Remove(credential);
                }
            }
            catch (Exception ex) when (ex.HResult == -2147023728)
            {
                return;
            }
        }

        public void SetCredential(string username, string passwordHash)
        {
            _ = username ?? throw new ArgumentNullException(nameof(username));
            _ = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
            vault.Add(new PasswordCredential("User", username, passwordHash));
        }

        private readonly PasswordVault vault = new PasswordVault();
    }
}
