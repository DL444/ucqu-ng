using System;

namespace DL444.Ucqu.App.WinUniversal.Exceptions
{
    internal class BackendRequestFailedException : Exception
    {
        public BackendRequestFailedException(string message) : base(message) { }

        public BackendRequestFailedException(string message, Exception innerException) : base(message, innerException) { }

        public BackendRequestFailedException(string message, Exception innerException, string displayMessage)
            : this(message, innerException) => DisplayMessage = displayMessage;

        public string DisplayMessage { get; set; }
    }
}
