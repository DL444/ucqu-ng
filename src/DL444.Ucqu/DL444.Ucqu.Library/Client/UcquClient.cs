using System;
using System.Net.Http;

namespace DL444.Ucqu.Library.Client
{
    public partial class UcquClient
    {
        public UcquClient(HttpClient httpClient) => this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        private HttpClient httpClient;
    }
}
