using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DL444.Ucqu.App.WinUniversal.Exceptions
{
    internal class BackendAuthenticationFailedException : BackendRequestFailedException
    {
        public BackendAuthenticationFailedException(string message) : base(message) { }

        public BackendAuthenticationFailedException(string message, Exception innerException) : base(message, innerException) { }

        public BackendAuthenticationFailedException(string message, Exception innerException, string displayMessage) : base(message, innerException, displayMessage) { }
    }
}
