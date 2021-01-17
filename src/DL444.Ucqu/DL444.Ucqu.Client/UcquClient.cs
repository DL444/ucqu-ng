using System;
using System.Net;
using System.Net.Http;
using System.Text;

namespace DL444.Ucqu.Client
{
    public partial class UcquClient : IUcquClient
    {
        /// <summary>
        /// Creates a new instance of UCQU registrar site client.
        /// </summary>
        /// <param name="httpClient">
        /// The HttpClient object used to make HTTP requests.
        /// You should set the base address, and configure it so cookie container of the underlying handler is NOT used.
        /// </param>
        public UcquClient(HttpClient httpClient)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4381.0 Safari/537.36 Edg/89.0.760.0");
            this.httpClient = httpClient;
        }

        private HttpClient httpClient;
    }
}
