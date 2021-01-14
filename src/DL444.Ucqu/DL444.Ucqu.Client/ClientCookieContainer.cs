using System;
using System.Net;

namespace DL444.Ucqu.Client
{
    public class ClientCookieContainer
    {
        public ClientCookieContainer(Uri domain) => this.domain = domain;

        public CookieContainer CookieContainer { get; } = new CookieContainer();

        public void SetSessionId(string sessionId)
        {
            CookieContainer.Add(domain, new Cookie("ASP.NET_SessionId", sessionId));
        }

        private Uri domain;
    }
}
