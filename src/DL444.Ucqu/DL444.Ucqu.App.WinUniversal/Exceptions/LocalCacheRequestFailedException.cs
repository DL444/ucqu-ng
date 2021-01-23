using System;

namespace DL444.Ucqu.App.WinUniversal.Exceptions
{
    internal class LocalCacheRequestFailedException : Exception
    {
        public LocalCacheRequestFailedException(string message) : base(message) { }

        public LocalCacheRequestFailedException(string message, Exception innerException) : base(message, innerException) { }
    }
}
