using System;
using System.Threading.Tasks;
using DL444.Ucqu.Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Protocols;

namespace DL444.Ucqu.Backend.Bindings
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    internal class ClientAuthenticationResultAttribute : Attribute { }

    internal class ClientAuthenticationResultValueProvider : IValueProvider
    {
        public ClientAuthenticationResultValueProvider(bool result) => this.result = result;
        public Type Type => typeof(bool);
        public Task<object> GetValueAsync() => Task.FromResult<object>(result);
        public string ToInvokeString() => result.ToString();
        private readonly bool result;
    }

    internal class ClientAuthenticationResultBinding : IBinding
    {
        public ClientAuthenticationResultBinding(IClientAuthenticationService authService) => this.authService = authService;

        public bool FromAttribute => true;

        public Task<IValueProvider> BindAsync(object value, ValueBindingContext context)
            => Task.FromResult<IValueProvider>(new ClientAuthenticationResultValueProvider((bool)value));

        public Task<IValueProvider> BindAsync(BindingContext context)
        {
            if (context.BindingData.ContainsKey("req") && context.BindingData["req"] is HttpRequest request)
            {
                bool result = authService.Validate(request.HttpContext.Connection.ClientCertificate);
                return BindAsync(result, context.ValueContext);
            }
            else
            {
                return BindAsync(false, context.ValueContext);
            }
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor()
            {
                Name = "Parameter: ClientAuthenticationResult",
                DisplayHints = new ParameterDisplayHints()
                {
                    Prompt = "Client authentication result",
                    Description = "Extract and verify client certificate from HTTP requests."
                }
            };
        }

        private readonly IClientAuthenticationService authService;
    }

    internal class ClientAuthenticationResultBindingProvider : IBindingProvider
    {
        public ClientAuthenticationResultBindingProvider(IClientAuthenticationService authService) => this.authService = authService;
        public Task<IBinding> TryCreateAsync(BindingProviderContext context) => Task.FromResult<IBinding>(new ClientAuthenticationResultBinding(authService));
        private readonly IClientAuthenticationService authService;
    }

    internal class ClientAuthenticationResultExtensionConfigProvider : IExtensionConfigProvider
    {
        public ClientAuthenticationResultExtensionConfigProvider(IClientAuthenticationService authService) => this.authService = authService;
        public void Initialize(ExtensionConfigContext context) 
            => context.AddBindingRule<ClientAuthenticationResultAttribute>().Bind(new ClientAuthenticationResultBindingProvider(authService));
        private readonly IClientAuthenticationService authService;
    }
}
