using System;
using System.Net;
using System.Net.Http;
using System.Text;

namespace DL444.Ucqu.Client
{
    public partial class UcquClient
    {
        /// <summary>
        /// Creates a new instance of UCQU registrar site client.
        /// </summary>
        /// <param name="httpClient">
        /// The HttpClient object used to make HTTP requests.
        /// You should configure it so cookie container of the underlying handler is NOT used.
        /// </param>
        /// <param name="host">The host of target site. Not including the protocol name or any slashes.</param>
        public UcquClient(HttpClient httpClient, string host)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4381.0 Safari/537.36 Edg/89.0.760.0");
            // Do not use TLS because the registrar site does not support it.
            httpClient.BaseAddress = new Uri($"http://{host}/");

            this.httpClient = httpClient;
            this.host = host;
        }

        private HttpClient httpClient;
        private string host;
    }
}
