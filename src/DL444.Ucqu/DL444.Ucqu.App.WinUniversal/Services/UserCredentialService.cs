using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal class UserCredentialService : ICredentialService
    {
        public string Username => throw new NotImplementedException();

        public string PasswordHash => throw new NotImplementedException();

        public string Token { get; set; }

        public void ClearCredential()
        {
            throw new NotImplementedException();
        }

        public void SetCredential(string username, string passwordHash)
        {
            throw new NotImplementedException();
        }
    }
}
