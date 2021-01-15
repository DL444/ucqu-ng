using System;
using System.Net;
using System.Net.Http;
using System.Text;

namespace DL444.Ucqu.Client
{
    public partial class UcquClient
    {
        public UcquClient(HttpClient httpClient, ClientCookieContainer cookieContainer, string host)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

            this.httpClient = httpClient;
            this.cookieContainer = cookieContainer;
            this.host = host;
        }

        private HttpClient httpClient;
        private ClientCookieContainer cookieContainer;
        private string host;
    }
}
