using System;
using DL444.Ucqu.App.WinUniversal.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DL444.Ucqu.App.WinUniversal.Extensions
{
    internal static class ServiceCollectionExtension
    {
        public static void AddMessageHub<TMessage, TImplementation>(this IServiceCollection services)
            where TMessage : IMessage
            where TImplementation : class, IMessageService<TMessage>
        {
            services.AddSingleton<IMessageService<TMessage>, TImplementation>();
        }

        public static void AddHttpClientWithDefaultPolicy<TService, TImplementation>(this IServiceCollection services, Uri baseAddress, int retryCount, int timeout)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddHttpClient<TService, TImplementation>(client =>
            {
                client.BaseAddress = baseAddress;
            }).AddDefaultPolicy(retryCount, timeout);
        }
    }
}
