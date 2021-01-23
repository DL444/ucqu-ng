using System;

namespace DL444.Ucqu.App.WinUniversal.Exceptions
{
    internal class LocalCacheInexistException : LocalCacheRequestFailedException
    {
        public LocalCacheInexistException(string message) : base(message) { }

        public LocalCacheInexistException(string message, Exception innerException) : base(message, innerException) { }
    }
}
