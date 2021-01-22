using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace DL444.Ucqu.App.WinUniversal.Extensions
{
    internal static class HttpClientBuilderExtension
    {
        public static IHttpClientBuilder AddDefaultPolicy(this IHttpClientBuilder builder, int retryCount, int timeout)
        {
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .WaitAndRetryAsync(retryCount, x => TimeSpan.FromSeconds(2 << x));
            var timeoutPolicy = Policy
                .TimeoutAsync<HttpResponseMessage>(timeout);
            return builder
                .AddPolicyHandler(retryPolicy)
                .AddPolicyHandler(timeoutPolicy);
        }
    }
}
