using System;
using System.Text.Json;
using System.Threading.Tasks;
using DL444.Ucqu.Backend.Services;
using DL444.Ucqu.Client;
using DL444.Ucqu.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public class SignIn
    {
        public SignIn(ITokenService tokenService, IUcquClient client, IDataAccessService dataService, ILocalizationService localizationService, IConfiguration config)
        {
            this.tokenService = tokenService;
            this.client = client;
            this.dataService = dataService;
            this.locService = localizationService;
            serviceBaseAddress = config.GetValue<string>("Host:ServiceBaseAddress");
        }

        [FunctionName("SignIn")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [Queue("user-init-queue", Connection = "Storage:ConnectionString")] IAsyncCollector<Models.UserInitializeCommand> userInitCommandCollector,
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
                Task<string>? initUserTask = null;
                if (credentialFetchResult.Success)
                {
                    shouldUpdateCredential = !credentialFetchResult.Resource.PasswordHash.Equals(credential.PasswordHash, StringComparison.Ordinal);
                }
                else if (credentialFetchResult.StatusCode == 404)
                {
                    // User does not previously exist.
                    shouldUpdateCredential = true;
                    initUserTask = StartInitializeUserAsync(signInContext, userInitCommandCollector, log);
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
                if (initUserTask != null)
                {
                    string location = await initUserTask;
                    return new AcceptedResult(location, new BackendResult<AccessToken>(AccessToken.IncompleteToken(token, location)));
                }
                else
                {
                    return new OkObjectResult(new BackendResult<AccessToken>(AccessToken.CompletedToken(token)));
                }
            }
            else if (signInContext.Result == Client.SignInResult.InvalidCredentials)
            {
                return new UnauthorizedResult();
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
                    return new UnauthorizedResult();
                }
            }
            else
            {
                return new OkObjectResult(new BackendResult<AccessToken>(failMessage));
            }
        }

        private async Task<string> StartInitializeUserAsync(SignInContext signInContext, IAsyncCollector<Models.UserInitializeCommand> collector, ILogger log)
        {
            string id = Guid.NewGuid().ToString();
            string location = $"{serviceBaseAddress}/UserInit/{id}";
            UserInitializeStatus status = new UserInitializeStatus(id, false, locService.GetString("UserInitPrepare"));
            DataAccessResult entryAddResult = await dataService.SetUserInitializeStatusAsync(status);
            if (!entryAddResult.Success)
            {
                log.LogWarning("Cannot add user initialize status to database. Status {statusCode}", entryAddResult.StatusCode);
            }
            try
            {
                await collector.AddAsync(new Models.UserInitializeCommand(signInContext, id));
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Cannot trigger user initialization.");
            }
            return location;
        }

        private ITokenService tokenService;
        private IUcquClient client;
        private IDataAccessService dataService;
        private ILocalizationService locService;
        private string serviceBaseAddress;
    }
}
