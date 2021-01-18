using System;
using System.Threading.Tasks;
using DL444.Ucqu.Backend.Bindings;
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
    public class Score
    {
        public Score(ITokenService tokenService, IUcquClient client, IDataAccessService dataService, ILocalizationService locService)
        {
            this.client = client;
            this.dataService = dataService;
            this.locService = locService;
        }

        [FunctionName("Score")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Score/{secondMajor:int?}")] HttpRequest req,
            int? secondMajor,
            [UserIdentity] string? username,
            ILogger log)
        {
            if (username == null)
            {
                return new UnauthorizedResult();
            }
            bool isSecondMajor;
            if (secondMajor == null || secondMajor == 0 || secondMajor == 1)
            {
                isSecondMajor = false;
            }
            else if (secondMajor == 2)
            {
                isSecondMajor = true;
            }
            else
            {
                return new NotFoundResult();
            }

            DataAccessResult<ScoreSet> result = await dataService.GetScoreAsync(username, isSecondMajor);
            if (result.Success)
            {
                // Got cached result.
                ScoreSet resource = result.Resource;
                if (resource.RecordStatus == RecordStatus.StaleAuthError)
                {
                    // User changed password upstream.
                    return new UnauthorizedResult();
                }
                else if (resource.RecordStatus == RecordStatus.StaleUpstreamError)
                {
                    // Upstream error, but we have cache.
                    return new OkObjectResult(new BackendResult<ScoreSet>(true, result.Resource, locService.GetString("CachedDataNotUpToDate")));
                }
                else
                {
                    // OK.
                    return new OkObjectResult(new BackendResult<ScoreSet>(true, result.Resource, null));
                }
            }
            else
            {
                // Did not get cached result. No cache or data access error.
                DataAccessResult<StudentCredential> credentialResult = await dataService.GetCredentialAsync(username);
                if (credentialResult.Success)
                {
                    // User exist.
                    try
                    {
                        SignInContext signInContext = await client.SignInAsync(credentialResult.Resource.StudentId, credentialResult.Resource.PasswordHash);
                        ScoreSet scoreSet = await client.GetScoreAsync(signInContext, isSecondMajor);
                        if (scoreSet.Terms.Count > 0)
                        {
                            // Be conservative here, since we do not have access to the old to see if the score could actually be empty.
                            DataAccessResult scoreUpdateResult = await dataService.SetScoreAsync(scoreSet);
                            if (!scoreUpdateResult.Success)
                            {
                                log.LogError("Unable to update score store. Status {statusCode}", scoreUpdateResult.StatusCode);
                            }
                        }
                        return new OkObjectResult(new BackendResult<ScoreSet>(true, scoreSet, null));
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Exception encountered while signing or fetching scores.");
                        return new OkObjectResult(new BackendResult<ScoreSet>(false, null, locService.GetString("UpstreamErrorCannotFetch")));
                    }
                }
                else if (credentialResult.StatusCode == 404 || credentialResult.StatusCode == 1)
                {
                    // User does not exist, or encryption key changed.
                    return new UnauthorizedResult();
                }
                else
                {
                    // Data access error.
                    log.LogError("Data access error occured fetching user credential. Status {statusCode}", credentialResult.StatusCode);
                    return new OkObjectResult(new BackendResult<ScoreSet>(false, null, locService.GetString("ServiceErrorCannotFetch")));
                }
            }
        }

        private IUcquClient client;
        private IDataAccessService dataService;
        private ILocalizationService locService;
    }
}
