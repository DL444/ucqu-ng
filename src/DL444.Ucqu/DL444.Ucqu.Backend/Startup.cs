using System;
using Azure.Cosmos;
using DL444.Ucqu.Backend.Models;
using DL444.Ucqu.Backend.Services;
using DL444.Ucqu.Client;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

[assembly: FunctionsStartup(typeof(DL444.Ucqu.Backend.Startup))]
namespace DL444.Ucqu.Backend
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var context = builder.GetContext();
            var config = context.Configuration;

            builder.Services.AddTransient<ITokenService, TokenService>();

            builder.Services.AddTransient<ICredentialEncryptionService, CredentialEncryptionService>();

            var host = config.GetValue<string>("Upstream:Host");
            bool useTls = config.GetValue<bool>("Upstream:UseTls", false);
            int timeout = config.GetValue<int>("Upstream:Timeout", 30);
            builder.Services.AddHttpClient<IUcquClient, UcquClient>(httpClient =>
            {
                httpClient.BaseAddress = new Uri($"{(useTls ? "https" : "http")}://{host}/");
                httpClient.Timeout = TimeSpan.FromSeconds(timeout);
            }).ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.HttpClientHandler()
            {
                UseCookies = false
            });

            var dbConnection = config.GetValue<string>("Database:ConnectionString");
            builder.Services.AddSingleton<CosmosClient>(_ => new CosmosClient(dbConnection));

            builder.Services.AddTransient<IDataAccessService, DataAccessService>();

            builder.Services.AddTransient<IPushDataAccessService, DataAccessService>();

            builder.Services.AddSingleton<ILocalizationService, LocalizationService>();

            builder.Services.AddSingleton<IWellknownDataService, WellknownDataService>();

            builder.Services.AddTransient<IGetFunctionHandlerService, GetFunctionHandlerService>();

            builder.Services.AddTransient<IRefreshFunctionHandlerService, RefreshFunctionHandlerService>();

            builder.Services.AddTransient<ICalendarService, CalendarService>();

            int retry = config.GetValue<int>("Notification:Retry", 2);
            builder.Services.AddHttpClient<IPushNotificationService<WindowsPushNotification>, WindowsPushNotificationService>()
                .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(retry, x => TimeSpan.FromSeconds(2 << x)));

            IWebJobsBuilder webJobsBuilder = builder.Services.AddWebJobs(_ => { });
            webJobsBuilder.AddExtension<Bindings.UserIdentityExtensionConfigProvider>();
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var context = builder.GetContext();
            builder.ConfigurationBuilder
                .AddJsonFile(System.IO.Path.Combine(context.ApplicationRootPath, "local.settings.json"), true)
                .AddJsonFile(System.IO.Path.Combine(context.ApplicationRootPath, "localization.json"))
                .AddJsonFile(System.IO.Path.Combine(context.ApplicationRootPath, "wellknown.json"))
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
