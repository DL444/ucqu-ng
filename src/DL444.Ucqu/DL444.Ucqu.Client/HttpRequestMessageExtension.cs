using System.Net.Http;

namespace DL444.Ucqu.Client
{
    public static class HttpRequestMessageExtension
    {
        public static HttpRequestMessage AddSessionCookie(this HttpRequestMessage request, string sessionId)
        {
            request.Headers.Add("Cookie", $"ASP.NET_SessionId={sessionId}");
            return request;
        }
    }
}
