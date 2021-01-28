using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DL444.Ucqu.Backend.Services;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Protocols;

namespace DL444.Ucqu.Backend.Bindings
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    internal class UserIdentityAttribute : Attribute { }

    internal class UserIdentityValueProvider : IValueProvider
    {
        public UserIdentityValueProvider(string? identity) => this.identity = identity;
        public Type Type => typeof(string);
        public Task<object?> GetValueAsync() => Task.FromResult<object?>(identity);
        public string ToInvokeString() => identity ?? string.Empty;
        private string? identity;
    }

    internal class UserIdentityBinding : IBinding
    {
        public UserIdentityBinding(ITokenService tokenService) => this.tokenService = tokenService;

        public bool FromAttribute => true;

        public Task<IValueProvider> BindAsync(object? value, ValueBindingContext context)
            => Task.FromResult<IValueProvider>(new UserIdentityValueProvider((string?)value));

        public Task<IValueProvider> BindAsync(BindingContext context)
        {
            bool headerExists = context.BindingData.TryGetValue("Headers", out object? headersObj);
            if (headerExists && headersObj is Dictionary<string, string> headers)
            {
                bool tokenExists = headers.TryGetValue("Authorization", out string? authHeader);
                if (!tokenExists)
                {
                    return BindAsync(null, context.ValueContext);
                }
                var tokenRegex = new Regex("^Bearer (\\S+)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
                Match tokenMatch = tokenRegex.Match(authHeader);
                if (!tokenMatch.Success)
                {
                    return BindAsync(null, context.ValueContext);
                }
                string token = tokenMatch.Groups[1].Value;
                return BindAsync(tokenService.ReadToken(token), context.ValueContext);
            }
            else
            {
                return BindAsync(null, context.ValueContext);
            }
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor()
            {
                Name = "Parameter: UserIdentity",
                DisplayHints = new ParameterDisplayHints()
                {
                    Prompt = "User Identity",
                    Description = "Extract user identity info from HTTP requests."
                }
            };
        }

        private ITokenService tokenService;
    }

    internal class UserIdentityBindingProvider : IBindingProvider
    {
        public UserIdentityBindingProvider(ITokenService tokenService) => this.tokenService = tokenService;
        public Task<IBinding> TryCreateAsync(BindingProviderContext context) => Task.FromResult<IBinding>(new UserIdentityBinding(tokenService));
        private ITokenService tokenService;
    }

    internal class UserIdentityExtensionConfigProvider : IExtensionConfigProvider
    {
        public UserIdentityExtensionConfigProvider(ITokenService tokenService) => this.tokenService = tokenService;
        public void Initialize(ExtensionConfigContext context)
            => context.AddBindingRule<UserIdentityAttribute>().Bind(new UserIdentityBindingProvider(tokenService));
        private ITokenService tokenService;
    }
}
