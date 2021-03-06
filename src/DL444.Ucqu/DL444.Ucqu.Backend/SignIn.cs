using System;
using System.Text.Json;
using System.Threading.Tasks;
using DL444.Ucqu.Backend.Models;
using DL444.Ucqu.Backend.Services;
using DL444.Ucqu.Client;
using DL444.Ucqu.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public class SignInFunction
    {
        public SignInFunction(ITokenService tokenService, IUcquClient client, IDataAccessService dataService, ILocalizationService localizationService, IConfiguration config)
        {
            this.tokenService = tokenService;
            this.client = client;
            this.dataService = dataService;
            this.locService = localizationService;
            serviceBaseAddress = config.GetValue<string>("Host:ServiceBaseAddress");
        }

        [FunctionName("SignIn")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "signIn/{createAccount:bool}")] HttpRequest req,
            [EventGrid(TopicEndpointUri = "EventPublish:TopicUri", TopicKeySetting = "EventPublish:TopicKey")] IAsyncCollector<EventGridEvent> userInitCommandCollector,
            bool createAccount,
            ILogger log)
        {
            StudentCredential? credential = null;
            try
            {
                credential = await JsonSerializer.DeserializeAsync<StudentCredential>(req.Body);
            }
            catch (JsonException)
            {
                return new BadRequestResult();
            }
            if (credential == null || credential.StudentId == null || credential.PasswordHash == null)
            {
                return new BadRequestResult();
            }

            SignInContext signInContext;
            try
            {
                signInContext = await client.SignInAsync(credential.StudentId, credential.PasswordHash);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Exception encountered while signing in to upstream server.");
                return await HandleUpstreamFailoverAsync(credential, locService.GetString("UpstreamErrorShowCached"), locService.GetString("UpstreamErrorCannotSignIn"), log);
            }
            if (signInContext.Result == Client.SignInResult.Success)
            {
                DataAccessResult<StudentCredential> credentialFetchResult = await dataService.GetCredentialAsync(credential.StudentId);
                bool shouldUpdateCredential = false;
                bool shouldInitializeUser = false;
                if (credentialFetchResult.Success)
                {
                    shouldUpdateCredential = !credentialFetchResult.Resource.PasswordHash.Equals(credential.PasswordHash, StringComparison.Ordinal);
                }
                else if (credentialFetchResult.StatusCode == 404)
                {
                    // User does not previously exist.
                    if (createAccount)
                    {
                        shouldUpdateCredential = true;
                        shouldInitializeUser = true;
                    }
                    else
                    {
                        return new UnauthorizedObjectResult(new BackendResult<AccessToken>(locService.GetString("AccountInexistCannotSignIn")));
                    }
                }
                else
                {
                    log.LogError("Unable to fetch user credential. Status {statusCode}", credentialFetchResult.StatusCode);
                }

                if (shouldUpdateCredential)
                {
                    DataAccessResult credentialUpdateResult = await dataService.SetCredentialAsync(credential);
                    if (!credentialUpdateResult.Success)
                    {
                        log.LogError("Unable to update user credential store. Status {statusCode}", credentialUpdateResult.StatusCode);
                    }
                }

                string token = tokenService.CreateToken(credential.StudentId);
                if (shouldInitializeUser)
                {
                    string location = await StartInitializeUserAsync(signInContext, userInitCommandCollector, log);
                    return new AcceptedResult(location, new BackendResult<AccessToken>(true, AccessToken.IncompleteToken(token, location), locService.GetString("UserInitPrepare")));
                }
                else
                {
                    return new OkObjectResult(new BackendResult<AccessToken>(AccessToken.CompletedToken(token)));
                }
            }
            else if (signInContext.Result == Client.SignInResult.InvalidCredentials)
            {
                return new UnauthorizedObjectResult(new BackendResult<AccessToken>(locService.GetString("CredentialErrorCannotSignIn")));
            }
            else if (signInContext.Result == Client.SignInResult.InvalidCredentialsUserInexist)
            {
                return new UnauthorizedObjectResult(new BackendResult<AccessToken>(locService.GetString("UserInexistCannotSignIn")));
            }
            else
            {
                if (signInContext.Result != Client.SignInResult.NotRegistered)
                {
                    log.LogError("Unexpected sign in result from client. Got {signInResult}", signInContext.ToString());
                }
                return await HandleUpstreamFailoverAsync(credential, locService.GetString("UpstreamUnregisteredShowCached"), locService.GetString("UpstreamErrorCannotSignIn"), log);
            }
        }

        private async Task<IActionResult> HandleUpstreamFailoverAsync(StudentCredential credential, string successMessage, string failMessage, ILogger log)
        {
            log.LogWarning("Experiencing issues with upstream service.");
            DataAccessResult<StudentCredential> credentialFetchResult = await dataService.GetCredentialAsync(credential.StudentId);
            if (credentialFetchResult.Success)
            {
                if (credential.PasswordHash.Equals(credentialFetchResult.Resource.PasswordHash, StringComparison.Ordinal))
                {
                    string token = tokenService.CreateToken(credential.StudentId);
                    return new OkObjectResult(new BackendResult<AccessToken>(true, AccessToken.CompletedToken(token), successMessage));
                }
                else
                {
                    return new UnauthorizedObjectResult(new BackendResult<AccessToken>(locService.GetString("CredentialErrorCannotSignIn")));
                }
            }
            else
            {
                return new UnauthorizedObjectResult(new BackendResult<AccessToken>(failMessage));
            }
        }

        private async Task<string> StartInitializeUserAsync(SignInContext signInContext, IAsyncCollector<EventGridEvent> collector, ILogger log)
        {
            string id = Guid.NewGuid().ToString();
            string location = $"{serviceBaseAddress}/UserInit/{id}";
            UserInitializeStatus status = new UserInitializeStatus(id, false);
            DataAccessResult entryAddResult = await dataService.SetUserInitializeStatusAsync(status);
            if (!entryAddResult.Success)
            {
                log.LogWarning("Cannot add user initialize status to database. Status {statusCode}", entryAddResult.StatusCode);
            }
            try
            {
                var eventGridEvent = new EventGridEvent(
                    id: $"UserInit-{Guid.NewGuid()}",
                    subject: signInContext.SignedInUser,
                    data: new Models.UserInitializeCommand(signInContext, id),
                    eventType: "DL444.Ucqu.UserInit",
                    eventTime: DateTime.UtcNow,
                    dataVersion: "1.0"
                );
                await collector.AddAsync(eventGridEvent);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Cannot trigger user initialization.");
            }
            return location;
        }

        private readonly ITokenService tokenService;
        private readonly IUcquClient client;
        private readonly IDataAccessService dataService;
        private readonly ILocalizationService locService;
        private readonly string serviceBaseAddress;
    }
}
