using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DL444.Ucqu.Backend.Models;
using DL444.Ucqu.Client;
using DL444.Ucqu.Models;
using Microsoft.Extensions.Logging;

namespace DL444.Ucqu.Backend.Services
{
    public interface IRefreshFunctionHandlerService
    {
        Task HandleRequestAsync<T>(
            string username,
            Func<IDataAccessService, Task<DataAccessResult<T>>> databaseFetchTask,
            Func<IUcquClient, SignInContext, Task<T>> clientFetchTask,
            Func<IDataAccessService, T, Task<DataAccessResult>> updateTask,
            Func<T?, T, bool> shouldUpdate,
            Func<T?, T, Task>? updateCompleteCallback,
            ILogger log) where T : IStatusResource;

        Task<List<string>?> GetUsersAsync(ILogger log);
    }

    internal class RefreshFunctionHandlerService : IRefreshFunctionHandlerService
    {
        public RefreshFunctionHandlerService(IUcquClient client, IDataAccessService dataService)
        {
            this.client = client;
            this.dataService = dataService;
        }

        public async Task HandleRequestAsync<T>(
            string username,
            Func<IDataAccessService, Task<DataAccessResult<T>>> databaseFetchTask,
            Func<IUcquClient, SignInContext, Task<T>> clientFetchTask,
            Func<IDataAccessService, T, Task<DataAccessResult>> updateTask,
            Func<T?, T, bool> shouldUpdate,
            Func<T?, T, Task>? updateCompleteCallback,
            ILogger log)
            where T : IStatusResource
        {
            // Fetch new version.
            Task<DataAccessResult<T>> oldFetchTask = databaseFetchTask(dataService);
            RecordStatus newStatus = RecordStatus.UpToDate;
            DataAccessResult<StudentCredential> credentialFetchResult = await dataService.GetCredentialAsync(username);
            T? newResource = default(T);
            if (credentialFetchResult.Success)
            {
                // Got credential.
                StudentCredential credential = credentialFetchResult.Resource;
                try
                {
                    SignInContext signInContext = await client.SignInAsync(credential.StudentId, credential.PasswordHash);
                    if (signInContext.IsValid)
                    {
                        // OK.
                        newResource = await clientFetchTask(client, signInContext);
                    }
                    else
                    {
                        // User changed password. Mark stale.
                        newStatus = RecordStatus.StaleAuthError;
                    }
                }
                catch (Exception ex)
                {
                    // New resource fetch failure. Mark stale.
                    log.LogError(ex, "Failed to sign in or fetch resource from upstream. Resource type {resourceType}", typeof(T));
                    newStatus = RecordStatus.StaleUpstreamError;
                }
            }
            else if (credentialFetchResult.StatusCode == 404)
            {
                // Credential lost. Mark stale.
                log.LogWarning("Credential not found. Resource type {resourceType}", typeof(T));
                newStatus = RecordStatus.StaleAuthError;
            }
            else
            {
                // Data access error. Skip this refresh.
                log.LogError("Failed to fetch credential. Resource type {resourceType}. Status {statusCode}", typeof(T), credentialFetchResult.StatusCode);
                return;
            }
            // End new version fetch.
            // As of now, either new version contains value, or stale status determined.

            // Fetch old version.
            T? oldResource = default(T);
            DataAccessResult<T> oldFetchResult = await oldFetchTask;
            if (oldFetchResult.Success)
            {
                // OK.
                oldResource = oldFetchResult.Resource;
            }
            else if (oldFetchResult.StatusCode != 404)
            {
                // Data access error.
                log.LogError("Failed to fetch resource from database. Resource type {resourceType}. Status {statusCode}", typeof(T), oldFetchResult.StatusCode);
                return;
            }
            // End old version fetch.
            // As of now, either old version contains value, or an old version really does not exist.

            // Compare and update.
            if (newResource == null && oldResource == null)
            {
                // Both new and old resources are missing. Skip for this time.
                log.LogWarning("Update skipped because both old and new resources are missing. Resource type {resourceType}.", typeof(T));
                return;
            }
            else if (newResource == null)
            {
                // New resource cannot be fetched. Update stale flag.
                oldResource!.RecordStatus = newStatus;
                DataAccessResult updateResult = await updateTask(dataService, oldResource);
                if (!updateResult.Success)
                {
                    log.LogError("Failed to update database. Resource type {resourceType}. Status {statusCode}", typeof(T), updateResult.StatusCode);
                }
                return;
            }
            else if (shouldUpdate(oldResource, newResource))
            {
                // Caller determined that the resource should be updated.
                DataAccessResult updateResult = await updateTask(dataService, newResource);
                if (updateResult.Success)
                {
                    if (updateCompleteCallback != null)
                    {
                        try
                        {
                            await updateCompleteCallback(oldResource, newResource);
                        }
                        catch (Exception ex)
                        {
                            log.LogWarning(ex, "Update completion callback ran into an error.");
                        }
                    }
                }
                else
                {
                    log.LogError("Failed to update database. Resource type {resourceType}. Status {statusCode}", typeof(T), updateResult.StatusCode);
                }
                return;
            }
            else if (oldResource != null && oldResource.RecordStatus != RecordStatus.UpToDate)
            {
                // Caller opted to not update, but stale flag has to be cleared.
                oldResource.RecordStatus = RecordStatus.UpToDate;
                DataAccessResult updateResult = await updateTask(dataService, oldResource);
                if (!updateResult.Success)
                {
                    log.LogError("Failed to update database. Resource type {resourceType}. Status {statusCode}", typeof(T), updateResult.StatusCode);
                }
                return;
            }
        }

        public async Task<List<string>?> GetUsersAsync(ILogger log)
        {
            try
            {
                DataAccessResult<List<string>> userFetchResult = await dataService.GetUsersAsync();
                return userFetchResult.Resource;
            }
            catch (Azure.Cosmos.CosmosException ex)
            {
                log.LogError(ex, "Cannot fetch user credentials. Status {statusCode}", ex.Status);
                return null;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Cannot fetch user credentials.");
                return null;
            }
        }

        private IUcquClient client;
        private IDataAccessService dataService;
    }
}
