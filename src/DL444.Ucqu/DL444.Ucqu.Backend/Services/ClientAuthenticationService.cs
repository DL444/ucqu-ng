using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;

namespace DL444.Ucqu.Backend.Services
{
    public interface IClientAuthenticationService
    {
        bool Validate(X509Certificate2 certificate);
    }

    internal class BypassClientAuthenticationService : IClientAuthenticationService
    {
        public bool Validate(X509Certificate2 certificate) => true;
    }

    internal class KeyVaultClientAuthenticationService : IClientAuthenticationService
    {
        public KeyVaultClientAuthenticationService(IConfiguration config)
        {
            disableChainValidation = config.GetValue<bool>("ClientAuthentication:DisableChainValidation");
            string certificateString = config.GetValue<string>("ClientAuthentication:CertificateReference");
            if (string.IsNullOrEmpty(certificateString))
            {
                throw new ArgumentException("Client certificate reference is not set.");
            }
            referenceThumbprint = new X509Certificate2(Convert.FromBase64String(certificateString)).Thumbprint;
        }

        public bool Validate(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                return false;
            }
            if (certificate.NotBefore > DateTime.Now || certificate.NotAfter < DateTime.Now)
            {
                return false;
            }
            if (!certificate.Thumbprint.Equals(referenceThumbprint))
            {
                return false;
            }
            return disableChainValidation ? true : certificate.Verify();
        }

        private readonly bool disableChainValidation;
        private readonly string referenceThumbprint;
    }
}
