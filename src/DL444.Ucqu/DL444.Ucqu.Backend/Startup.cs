using System;
using Azure.Cosmos;
using DL444.Ucqu.Backend.Models;
using DL444.Ucqu.Backend.Services;
using DL444.Ucqu.Client;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

[assembly: WebJobsStartup(typeof(DL444.Ucqu.Backend.Startup))]
namespace DL444.Ucqu.Backend
{
    // Adapted from Microsoft source, since stock implementation does not support WebJobs extensions.
    // See https://github.com/Azure/azure-functions-dotnet-extensions/blob/main/src/Extensions/DependencyInjection/FunctionsStartup.cs
    internal class Startup : IWebJobsStartup2, IWebJobsConfigurationStartup
    {
        public void Configure(IWebJobsBuilder builder) => Configure(new WebJobsBuilderContext(), builder);

        public void Configure(WebJobsBuilderContext context, IWebJobsBuilder builder)
        {
            var functionsBuilder = new FunctionsHostBuilder(builder.Services, context);
            Configure(functionsBuilder);
            ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
            var tokenService = serviceProvider.GetService<ITokenService>() ?? throw new NullReferenceException("Token service not returned by service provider.");
            builder.AddExtension(new Bindings.UserIdentityExtensionConfigProvider(tokenService));
        }

        public void Configure(WebJobsBuilderContext context, IWebJobsConfigurationBuilder builder)
        {
            var functionsConfigBuilder = new FunctionsConfigurationBuilder(builder.ConfigurationBuilder, context);
            ConfigureAppConfiguration(functionsConfigBuilder);
        }

        public void Configure(IFunctionsHostBuilder builder)
        {
            var context = builder is FunctionsHostBuilder fnBuilder ? fnBuilder.Context : builder.GetContext();
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

            int retry = config.GetValue<int>("Notification:Windows:Retry", 2);
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
        }

        public void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var context = builder is FunctionsConfigurationBuilder fnBuilder ? fnBuilder.Context : builder.GetContext();
            builder.ConfigurationBuilder
                .AddJsonFile(System.IO.Path.Combine(context.ApplicationRootPath, "local.settings.json"), true)
                .AddJsonFile(System.IO.Path.Combine(context.ApplicationRootPath, "localization.json"))
                .AddJsonFile(System.IO.Path.Combine(context.ApplicationRootPath, "wellknown.json"))
                .AddEnvironmentVariables()
                .Build();
        }
    }

    internal class FunctionsHostBuilder : IFunctionsHostBuilder, IFunctionsHostBuilderExt
    {
        public FunctionsHostBuilder(IServiceCollection services, WebJobsBuilderContext webJobsBuilderContext)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Context = new DefaultFunctionsHostBuilderContext(webJobsBuilderContext);
        }

        public IServiceCollection Services { get; }
        public FunctionsHostBuilderContext Context { get; }
    }

    internal interface IFunctionsHostBuilderExt
    {
        FunctionsHostBuilderContext Context { get; }
    }

    internal class DefaultFunctionsHostBuilderContext : FunctionsHostBuilderContext
    {
        public DefaultFunctionsHostBuilderContext(WebJobsBuilderContext webJobsBuilderContext) : base(webJobsBuilderContext) { }
    }

    internal class FunctionsConfigurationBuilder : IFunctionsConfigurationBuilder, IFunctionsHostBuilderExt
    {
        public FunctionsConfigurationBuilder(IConfigurationBuilder configurationBuilder, WebJobsBuilderContext webJobsBuilderContext)
        {
            ConfigurationBuilder = configurationBuilder ?? throw new ArgumentNullException(nameof(configurationBuilder));
            Context = new DefaultFunctionsHostBuilderContext(webJobsBuilderContext);
        }

        public IConfigurationBuilder ConfigurationBuilder { get; }
        public FunctionsHostBuilderContext Context { get; }
    }
}
