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

            var tokenSigningKey = config.GetValue<string>("Token:SigningKey");
            var tokenIssuer = config.GetValue<string>("Token:Issuer");
            var tokenValidMins = config.GetValue<int>("Token:ValidMinutes", 60);
            builder.Services.AddTransient<ITokenService>(_ => new TokenService(tokenSigningKey, tokenIssuer, tokenValidMins));

            var credentialKey = config.GetValue<string>("Credential:EncryptionKey");
            builder.Services.AddTransient<ICredentialEncryptionService>(_ => new CredentialEncryptionService(credentialKey));

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
            builder.Services.AddSingleton(new CosmosClient(dbConnection));

            builder.Services.AddTransient<IDataAccessService, DataAccessService>();

            builder.Services.AddTransient<IPushDataAccessService, DataAccessService>();

            builder.Services.AddSingleton((ILocalizationService)new LocalizationService(config.GetSection("Localization")));

            builder.Services.AddSingleton((IWellknownDataService)new WellknownDataService(config));

            builder.Services.AddTransient<IGetFunctionHandlerService, GetFunctionHandlerService>();

            builder.Services.AddTransient<IRefreshFunctionHandlerService, RefreshFunctionHandlerService>();

            builder.Services.AddTransient<ICalendarService, CalendarService>();

            int retry = config.GetValue<int>("Notification:Retry", 2);
            builder.Services.AddHttpClient<IPushNotificationService<WindowsPushNotification>, WindowsPushNotificationService>()
                .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(retry, x => TimeSpan.FromSeconds(2 << x)));

            bool clientCertificateValidationEnabled = config.GetValue("ClientAuthentication:Enabled", false);
            if (clientCertificateValidationEnabled)
            {
                builder.Services.AddTransient<IClientAuthenticationService, KeyVaultClientAuthenticationService>();
            }
            else
            {
                builder.Services.AddTransient<IClientAuthenticationService, BypassClientAuthenticationService>();
            }

            IWebJobsBuilder webJobsBuilder = builder.Services.AddWebJobs(_ => { });
            webJobsBuilder.AddExtension<Bindings.UserIdentityExtensionConfigProvider>();
            webJobsBuilder.AddExtension<Bindings.ClientAuthenticationResultExtensionConfigProvider>();
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
