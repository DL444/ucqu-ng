using System;
using Azure.Cosmos;
using DL444.Ucqu.Backend.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            builder.Services.AddHttpClient<DL444.Ucqu.Client.IUcquClient, DL444.Ucqu.Client.UcquClient>(httpClient =>
            {
                httpClient.BaseAddress = new Uri($"{(useTls ? "https" : "http")}://{host}/");
            }).ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.HttpClientHandler()
            {
                UseCookies = false
            });

            var dbConnection = config.GetValue<string>("Database:ConnectionString");
            builder.Services.AddSingleton(new CosmosClient(dbConnection));

            var databaseId = config.GetValue<string>("Database:Database");
            var containerId = config.GetValue<string>("Database:Container");
            builder.Services.AddTransient<IDataAccessService>(
                services => new DataAccessService(services.GetService<CosmosClient>(), databaseId, containerId, services.GetService<ICredentialEncryptionService>())
            );
        }

        public void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var context = builder is FunctionsConfigurationBuilder fnBuilder ? fnBuilder.Context : builder.GetContext();
            builder.ConfigurationBuilder
                .AddJsonFile(System.IO.Path.Combine(context.ApplicationRootPath, "local.settings.json"), true)
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
