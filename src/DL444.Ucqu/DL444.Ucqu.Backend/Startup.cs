using System;
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
            var serviceProvider = builder.Services.BuildServiceProvider();
            builder.AddExtension(new Bindings.UserIdentityExtensionConfigProvider(serviceProvider.GetService<ITokenService>()));
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
            var tokenValidMins = config.GetValue<int>("Token:ValidMinutes");
            builder.Services.AddSingleton<ITokenService>(new TokenService(tokenSigningKey, tokenIssuer, tokenValidMins));
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
