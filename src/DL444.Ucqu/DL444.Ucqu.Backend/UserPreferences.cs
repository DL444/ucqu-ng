using System;
using System.Collections.Generic;
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
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend
{
    public class UserPreferencesFunction
    {
        public UserPreferencesFunction(IDataAccessService dataService, ILocalizationService locService)
        {
            this.dataService = dataService;
            this.locService = locService;
        }

        [FunctionName("UserPreferencesGet")]
        public async Task<IActionResult> RunGet(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "preferences")] HttpRequest req,
            [ClientAuthenticationResult] bool clientAuthSuccess,
            [UserIdentity] string? username,
            ILogger log)
        {
            if (!clientAuthSuccess || username == null)
            {
                return new UnauthorizedResult();
            }

            DataAccessResult<UserPreferences> result = await dataService.GetUserPreferences(username);
            if (result.Success)
            {
                return new OkObjectResult(new BackendResult<UserPreferences>(result.Resource));
            }
            else if (result.StatusCode == 404)
            {
                DataAccessResult<StudentCredential> fetchUserResult = await dataService.GetCredentialAsync(username);
                if (fetchUserResult.Success)
                {
                    UserPreferences preferences = new UserPreferences(username);
                    DataAccessResult createPreferencesResult = await dataService.SetUserPreferences(preferences);
                    if (!createPreferencesResult.Success)
                    {
                        log.LogWarning("Failed to create preference record in database. Username: {username}, Status {statusCode}", username, createPreferencesResult.StatusCode);
                    }
                    return new OkObjectResult(new BackendResult<UserPreferences>(preferences));
                }
                else if (fetchUserResult.StatusCode == 404)
                {
                    return new UnauthorizedResult();
                }
                else
                {
                    log.LogError("Failed to fetch user credentials from database. Username: {username}, Status {statusCode}", username, result.StatusCode);
                    return GetFailedResult(false);
                }
            }
            else
            {
                log.LogError("Failed to fetch user preferences from database. Username: {username}, Status {statusCode}", username, result.StatusCode);
                return GetFailedResult(false);
            }
        }

        [FunctionName("UserPreferencesPost")]
        public async Task<IActionResult> RunPost(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "preferences")] HttpRequest req,
            [ClientAuthenticationResult] bool clientAuthSuccess,
            [UserIdentity] string? username,
            ILogger log)
        {
            if (!clientAuthSuccess || username == null)
            {
                return new UnauthorizedResult();
            }

            UserPreferences? inPreferences;
            try
            {
                inPreferences = await JsonSerializer.DeserializeAsync<UserPreferences>(req.Body);
            }
            catch (JsonException)
            {
                return new BadRequestResult();
            }
            if (inPreferences == null || !username.Equals(inPreferences.Username, StringComparison.Ordinal) || inPreferences.PreferenceItems == null)
            {
                return new BadRequestResult();
            }

            DataAccessResult<UserPreferences> result = await dataService.GetUserPreferences(username);
            if (result.Success)
            {
                UserPreferences newPreferences = UpdateUserPreference(result.Resource, inPreferences);
                DataAccessResult updateResult = await dataService.SetUserPreferences(newPreferences);
                if (updateResult.Success)
                {
                    return new OkObjectResult(new BackendResult<UserPreferences>(newPreferences));
                }
                else
                {
                    log.LogError("Failed to update user preferences. Username: {username}, Status: {statusCode}", username, updateResult.StatusCode);
                    return GetFailedResult(true);
                }
            }
            else if (result.StatusCode == 404)
            {
                DataAccessResult<StudentCredential> fetchUserResult = await dataService.GetCredentialAsync(username);
                if (fetchUserResult.Success)
                {
                    DataAccessResult createPreferencesResult = await dataService.SetUserPreferences(inPreferences);
                    if (createPreferencesResult.Success)
                    {
                        string json = SerializeResult(new BackendResult<UserPreferences>(inPreferences));
                        return new OkObjectResult(new BackendResult<UserPreferences>(inPreferences));
                    }
                    else
                    {
                        log.LogWarning("Failed to create preference record in database. Username: {username}, Status {statusCode}", username, createPreferencesResult.StatusCode);
                        return GetFailedResult(true);
                    }
                }
                else if (fetchUserResult.StatusCode == 404)
                {
                    return new UnauthorizedResult();
                }
                else
                {
                    log.LogError("Failed to fetch user credentials from database. Username: {username}, Status {statusCode}", username, result.StatusCode);
                    return GetFailedResult(true);
                }
            }
            else
            {
                log.LogError("Failed to fetch existing user preferences. Username: {username}, Status: {statusCode}", username, result.StatusCode);
                return GetFailedResult(true);
            }
        }

        private UserPreferences UpdateUserPreference(UserPreferences prev, UserPreferences curr)
        {
            foreach (KeyValuePair<string, string> item in curr.PreferenceItems)
            {
                if (prev.PreferenceItems.ContainsKey(item.Key))
                {
                    prev.PreferenceItems[item.Key] = item.Value;
                }
                else
                {
                    prev.PreferenceItems.Add(item.Key, item.Value);
                }
            }
            return prev;
        }

        private ObjectResult GetFailedResult(bool isUpdate)
        {
            return new ObjectResult(new BackendResult<UserPreferences>(locService.GetString(isUpdate ? "ServiceErrorCannotUpdatePreferences" : "ServiceErrorCannotFetchPreferences")))
            {
                StatusCode = 503
            };
        }

        private string SerializeResult(BackendResult<UserPreferences> preferences) => JsonSerializer.Serialize(preferences);

        private readonly IDataAccessService dataService;
        private readonly ILocalizationService locService;
    }
}
