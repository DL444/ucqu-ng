using System;
using System.Net;
using System.Net.Http;
using System.Text;

namespace DL444.Ucqu.Client
{
    public partial class UcquClient
    {
        public UcquClient(HttpClient httpClient, ClientCookieContainer cookieContainer, string host, string schoolCode)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            this.httpClient = httpClient;
            this.cookieContainer = cookieContainer;
            this.host = host;
            this.schoolCode = schoolCode;
        }

        private HttpClient httpClient;
        private ClientCookieContainer cookieContainer;
        private string host;
        private string schoolCode;
    }
}
