using System;
using System.Text.Json;
using System.Threading.Tasks;
using DL444.Ucqu.Backend.Bindings;
using DL444.Ucqu.Backend.Models;
using DL444.Ucqu.Backend.Services;
using DL444.Ucqu.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public class NotificationChannelWnsFunction
    {
        public NotificationChannelWnsFunction(IPushDataAccessService pushDataService, IDataAccessService userDataService, IConfiguration config, IClientAuthenticationService clientAuthService)
        {
            this.pushDataService = pushDataService;
            this.userDataService = userDataService;
            validChannelHost = config.GetValue<string>("Notification:Windows:ValidChannelHost");
            this.clientAuthService = clientAuthService;
        }

        [FunctionName("NotificationChannelWns")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "notifyChannel/windows")] HttpRequest req,
            [UserIdentity] string? username,
            ILogger log)
        {
            if (!clientAuthService.Validate(req.HttpContext.Connection.ClientCertificate))
            {
                return new ForbidResult();
            }
            if (username == null)
            {
                return new UnauthorizedResult();
            }
            NotificationChannelItem channel;
            try
            {
                channel = await JsonSerializer.DeserializeAsync<NotificationChannelItem>(req.Body);
            }
            catch (JsonException)
            {
                return new BadRequestResult();
            }
            if (!Uri.TryCreate(channel.ChannelIdentifier, UriKind.Absolute, out Uri? uri) || !uri.Host.EndsWith(validChannelHost, StringComparison.Ordinal))
            {
                return new BadRequestResult();
            }

            DataAccessResult<StudentCredential> userFetchResult = await userDataService.GetCredentialAsync(username);
            if (userFetchResult.StatusCode == 404)
            {
                return new UnauthorizedResult();
            }
            else if (!userFetchResult.Success)
            {
                log.LogError("Cannot fetch user information from database. Status: {statusCode}", userFetchResult.StatusCode);
                return new ObjectResult(new BackendResult<object>(false, new object(), null))
                {
                    StatusCode = 503
                };
            }

            DataAccessResult result = await pushDataService.AddPushChannelAsync(username, PushPlatform.Wns, channel.ChannelIdentifier);
            if (result.Success)
            {
                return new OkObjectResult(new BackendResult<object>(new object()));
            }
            else
            {
                log.LogError("Cannot write notification channel to database. Status: {statusCode}", result.StatusCode);
                return new ObjectResult(new BackendResult<object>(false, new object(), null))
                {
                    StatusCode = 503
                };
            }
        }

        private readonly IPushDataAccessService pushDataService;
        private readonly IDataAccessService userDataService;
        private readonly string validChannelHost;
        private readonly IClientAuthenticationService clientAuthService;
    }
}
