using System;
using System.Threading.Tasks;
using DL444.Ucqu.Backend.Services;
using DL444.Ucqu.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public class SignIn
    {
        public SignIn(ITokenService tokenService, IUcquClient client)
        {
            this.tokenService = tokenService;
            this.client = client;
        }

        [FunctionName("SignIn")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [Bindings.UserIdentity] string? id,
            ILogger log)
        {
            throw new NotImplementedException();
        }

        private ITokenService tokenService;
        private IUcquClient client;
    }
}
