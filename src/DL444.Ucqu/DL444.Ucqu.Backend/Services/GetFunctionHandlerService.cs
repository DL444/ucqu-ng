using System;
using System.Threading.Tasks;
using DL444.Ucqu.Backend.Models;
using DL444.Ucqu.Client;
using DL444.Ucqu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend.Services
{
    public interface IGetFunctionHandlerService
    {
        Task<IActionResult> HandleRequestAsync<T>(
            string username,
            Func<IDataAccessService, Task<DataAccessResult<T>>> databaseFetchTask,
            Func<IUcquClient, SignInContext, Task<T>> clientFetchTask,
            ILogger log) where T : IStatusResource;

        Task<IActionResult> HandleRequestAsync<T>(
            string username,
            Func<IDataAccessService, Task<DataAccessResult<T>>> databaseFetchTask,
            Func<IUcquClient, SignInContext, Task<T>> clientFetchTask,
            Func<IDataAccessService, T, Task<DataAccessResult>> writeBackTask,
            ILogger log) where T : IStatusResource;

        Task<IActionResult> HandleRequestAsync<T>(
            string username,
            Func<IDataAccessService, Task<DataAccessResult<T>>> databaseFetchTask,
            Func<IUcquClient, SignInContext, Task<T>> clientFetchTask,
            Func<IDataAccessService, T, Task<DataAccessResult>> writeBackTask,
            Predicate<T> shouldWriteBack,
            ILogger log) where T : IStatusResource;
    }

    internal class GetFunctionHandlerService : IGetFunctionHandlerService
    {
        public GetFunctionHandlerService(IUcquClient client, IDataAccessService dataService, ILocalizationService locService)
        {
            this.client = client;
            this.dataService = dataService;
            this.locService = locService;
        }

        public async Task<IActionResult> HandleRequestAsync<T>(
            string username,
            Func<IDataAccessService, Task<DataAccessResult<T>>> databaseFetchTask,
            Func<IUcquClient, SignInContext, Task<T>> clientFetchTask,
            ILogger log)
            where T : IStatusResource
        {
            return await HandleRequestAsync(username, databaseFetchTask, clientFetchTask, (x, y) => Task.FromResult(new DataAccessResult(true, 200)), _ => false, log);
        }

        public async Task<IActionResult> HandleRequestAsync<T>(
            string username,
            Func<IDataAccessService, Task<DataAccessResult<T>>> databaseFetchTask,
            Func<IUcquClient, SignInContext, Task<T>> clientFetchTask,
            Func<IDataAccessService, T, Task<DataAccessResult>> writeBackTask,
            ILogger log)
            where T : IStatusResource
        {
            return await HandleRequestAsync(username, databaseFetchTask, clientFetchTask, writeBackTask, _ => true, log);
        }

        public async Task<IActionResult> HandleRequestAsync<T>(
            string username,
            Func<IDataAccessService, Task<DataAccessResult<T>>> databaseFetchTask,
            Func<IUcquClient, SignInContext, Task<T>> clientFetchTask,
            Func<IDataAccessService, T, Task<DataAccessResult>> writeBackTask,
            Predicate<T> shouldWriteBack,
            ILogger log)
            where T : IStatusResource
        {
            DataAccessResult<T> result = await databaseFetchTask(dataService);
            if (result.Success)
            {
                // Got cached result.
                T resource = result.Resource;
                if (resource.RecordStatus == RecordStatus.UpToDate)
                {
                    // OK.
                    return new OkObjectResult(new BackendResult<T>(resource));
                }
                else if (resource.RecordStatus == RecordStatus.StaleAuthError)
                {
                    // User changed password upstream.
                    return new UnauthorizedResult();
                }
                else if (resource.RecordStatus == RecordStatus.StaleUpstreamError)
                {
                    // Upstream error, but we have cache.
                    return new OkObjectResult(new BackendResult<T>(true, resource, locService.GetString("CachedDataNotUpToDate")));
                }
                else
                {
                    log.LogCritical("Record status not implemented.");
                    throw new NotImplementedException();
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
                        if (signInContext.Result == Client.SignInResult.InvalidCredentials)
                        {
                            // Cached credential is outdated.
                            return new UnauthorizedResult();
                        }
                        else if (signInContext.Result == Client.SignInResult.NotRegistered)
                        {
                            // Upstream service is not accessible.
                            return new OkObjectResult(new BackendResult<ScoreSet>(locService.GetString("UpstreamUnregisteredCannotFetch")));
                        }
                        else
                        {
                            // Upstream OK.
                            T resource = await clientFetchTask(client, signInContext);
                            if (shouldWriteBack(resource))
                            {
                                DataAccessResult writeBackResult = await writeBackTask(dataService, resource);
                                if (!writeBackResult.Success)
                                {
                                    log.LogError("Unable to update database. Resource type {resType}, Status {statusCode}", typeof(T), writeBackResult.StatusCode);
                                }
                            }
                            return new OkObjectResult(new BackendResult<T>(resource));
                        }
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, "Exception encountered while signing in or fetching resource. Resource type {resType}", typeof(T));
                        return new OkObjectResult(new BackendResult<ScoreSet>(locService.GetString("UpstreamErrorCannotFetch")));
                    }
                }
                else if (credentialResult.StatusCode == 404)
                {
                    // User does not exist, or encryption key changed.
                    return new UnauthorizedResult();
                }
                else
                {
                    // Data access error.
                    log.LogError("Data access error occured fetching user credential. Status {statusCode}", credentialResult.StatusCode);
                    return new OkObjectResult(new BackendResult<ScoreSet>(locService.GetString("ServiceErrorCannotFetch")));
                }
            }
        }

        private IUcquClient client;
        private IDataAccessService dataService;
        private ILocalizationService locService;
    }
}
