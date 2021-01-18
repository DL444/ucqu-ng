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
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public class SignIn
    {
        public SignIn(ITokenService tokenService, IUcquClient client, IDataAccessService dataService, ILocalizationService localizationService)
        {
            this.tokenService = tokenService;
            this.client = client;
            this.dataService = dataService;
            this.locService = localizationService;
        }

        [FunctionName("SignIn")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
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

            Models.SignInResult signInResult;
            try
            {
                signInResult = await client.SignInAsync(credential.StudentId, credential.PasswordHash);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Exception encountered while signing in to upstream server.");
                return await HandleUpstreamFailoverAsync(credential, locService.GetString("UpstreamErrorShowCached"), locService.GetString("UpstreamErrorCannotSignIn"), log);
            }
            if (signInResult == Models.SignInResult.Success)
            {
                DataAccessResult credentialUpdateResult = await dataService.SetCredentialAsync(credential);
                if (!credentialUpdateResult.Success)
                {
                    log.LogError("Unable to update user credential store. Status {statusCode}", credentialUpdateResult.StatusCode);
                }
                string token = tokenService.CreateToken(credential.StudentId);
                return new OkObjectResult(new BackendResult<AccessToken>(new AccessToken(token)));
            }
            else if (signInResult == Models.SignInResult.InvalidCredentials)
            {
                return new UnauthorizedResult();
            }
            else
            {
                if (signInResult != Models.SignInResult.NotRegistered)
                {
                    log.LogError("Unexpected sign in result from client. Got {signInResult}", signInResult.ToString());
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
                    return new OkObjectResult(new BackendResult<AccessToken>(true, new AccessToken(token), successMessage));
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

        private ITokenService tokenService;
        private IUcquClient client;
        private IDataAccessService dataService;
        private ILocalizationService locService;
    }
}
