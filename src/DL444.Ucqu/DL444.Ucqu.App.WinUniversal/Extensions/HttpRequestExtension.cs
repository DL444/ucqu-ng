using System;
using System.Net.Http;

namespace DL444.Ucqu.App.WinUniversal.Extensions
{
    internal static class HttpRequestExtension
    {
        public static HttpRequestMessage AddToken(this HttpRequestMessage request, string token)
        {
            if (token != null)
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return request;
        }
    }
}
