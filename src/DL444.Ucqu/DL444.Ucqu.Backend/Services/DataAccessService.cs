using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Cosmos;
using DL444.Ucqu.Backend.Models;
using DL444.Ucqu.Models;
using Microsoft.Extensions.Configuration;

namespace DL444.Ucqu.Backend.Services
{
    public interface IDataAccessService
    {
        Task<DataAccessResult<List<string>>> GetUsersAsync();
        Task<DataAccessResult<StudentCredential>> GetCredentialAsync(string username);
        Task<DataAccessResult> SetCredentialAsync(StudentCredential credential);
        Task<DataAccessResult<UserInitializeStatus>> GetUserInitializeStatusAsync(string id);
        Task<DataAccessResult> SetUserInitializeStatusAsync(UserInitializeStatus status);
        Task<DataAccessResult> PurgeUserInitializeStatusAsync();
        Task<DataAccessResult<StudentInfo>> GetStudentInfoAsync(string username);
        Task<DataAccessResult> SetStudentInfoAsync(StudentInfo info);
        Task<DataAccessResult<Schedule>> GetScheduleAsync(string username);
        Task<DataAccessResult> SetScheduleAsync(Schedule schedule);
        Task<DataAccessResult<ExamSchedule>> GetExamsAsync(string username);
        Task<DataAccessResult> SetExamsAsync(ExamSchedule exams);
        Task<DataAccessResult<ScoreSet>> GetScoreAsync(string username, bool isSecondMajor);
        Task<DataAccessResult> SetScoreAsync(ScoreSet scoreSet);
        Task<DataAccessResult<UserPreferences>> GetUserPreferences(string username);
        Task<DataAccessResult> SetUserPreferences(UserPreferences preferences);

        Task<DataAccessResult> DeleteUserAsync(string username);
        Task<DataAccessResult<DeveloperMessage>> GetDeveloperMessageAsync();
    }

    public interface IPushDataAccessService
    {
        Task<DataAccessResult<PushAccessToken>> GetPushAccessTokenAsync(PushPlatform platform);
        Task<DataAccessResult> SetPushAccessTokenAsync(PushAccessToken token);
        Task<DataAccessResult<NotificationChannelCollection>> GetPushChannelsAsync(string username, PushPlatform platform);
        Task<DataAccessResult> AddPushChannelAsync(string username, PushPlatform platform, string channelIdentifier);
        Task<DataAccessResult> RemovePushChannelAsync(string username, PushPlatform platform, IEnumerable<string> channelIdentifiers);
    }

    internal class DataAccessService : IDataAccessService, IPushDataAccessService
    {
        public DataAccessService(CosmosClient dbClient, IConfiguration config, ICredentialEncryptionService encryptionService)
        {
            var databaseId = config.GetValue<string>("Database:Database");
            var containerId = config.GetValue<string>("Database:Container");
            container = dbClient.GetContainer(databaseId, containerId);
            this.encryptionService = encryptionService;
            maxChannelCountPerPlatform = config.GetValue<int>("Notification:MaxChannelCountPerPlatform");
        }

        public async Task<DataAccessResult<List<string>>> GetUsersAsync()
        {
            int userCount = 0;
            QueryDefinition countQuery = new QueryDefinition("SELECT COUNT(c) AS ItemCount FROM c WHERE c.pk = \"Credential\"");
            await foreach (Models.CountHeader header in container.GetItemQueryIterator<Models.CountHeader>(countQuery, requestOptions: new QueryRequestOptions()
            {
                PartitionKey = new PartitionKey("Credential")
            }))
            {
                userCount = header.ItemCount;
            }
            if (userCount == 0)
            {
                return DataAccessResult<List<string>>.Ok(new List<string>());
            }
            var users = new List<string>(userCount);
            QueryDefinition usersQuery = new QueryDefinition("SELECT c.Resource.StudentId FROM c WHERE c.pk = \"Credential\"");
            await foreach (Models.StudentIdHeader header in container.GetItemQueryIterator<Models.StudentIdHeader>(usersQuery, requestOptions: new QueryRequestOptions()
            {
                PartitionKey = new PartitionKey("Credential")
            }))
            {
                users.Add(header.StudentId);
            }
            return DataAccessResult<List<string>>.Ok(users);
        }

        public async Task<DataAccessResult<StudentCredential>> GetCredentialAsync(string username)
        {
            DataAccessResult<StudentCredential> fetchResult = await GetResourceAsync<StudentCredential>($"Credential-{username}", "Credential");
            if (fetchResult.Success)
            {
                try
                {
                    encryptionService.DecryptCredential(fetchResult.Resource);
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    // Key changed after last encryption. Equivalent to credential loss.
                    return new DataAccessResult<StudentCredential>(false, null, 404);
                }
            }
            return fetchResult;
        }
        public async Task<DataAccessResult> SetCredentialAsync(StudentCredential credential)
        {
            encryptionService.EncryptCredential(credential);
            return await SetResourceAsync(credential);
        }

        public Task<DataAccessResult<UserInitializeStatus>> GetUserInitializeStatusAsync(string id) => GetResourceAsync<UserInitializeStatus>($"UserInitStatus-{id}", "UserInitStatus");
        public Task<DataAccessResult> SetUserInitializeStatusAsync(UserInitializeStatus status) => SetResourceAsync(status);
        public async Task<DataAccessResult> PurgeUserInitializeStatusAsync()
        {
            long threshold = System.DateTimeOffset.UtcNow.AddMinutes(-15).ToUnixTimeMilliseconds();
            List<string> ids = new List<string>();
            QueryDefinition query = new QueryDefinition($"SELECT c.id FROM c WHERE c.pk = \"UserInitStatus\" AND c.Resource.LastUpdateTimestamp < {threshold}");
            await foreach (Models.IdHeader header in container.GetItemQueryIterator<Models.IdHeader>(query, requestOptions: new QueryRequestOptions()
            {
                PartitionKey = new PartitionKey("UserInitStatus")
            }))
            {
                ids.Add(header.Id);
            }
            bool hasError = false;
            foreach (string id in ids)
            {
                try
                {
                    await container.DeleteItemAsync<CosmosResource<UserInitializeStatus>>(id, new PartitionKey("UserInitStatus"));
                }
                catch (CosmosException)
                {
                    hasError = true;
                }
            }
            return new DataAccessResult(!hasError, 0);
        }

        public Task<DataAccessResult<StudentInfo>> GetStudentInfoAsync(string username) => GetResourceAsync<StudentInfo>($"Student-{username}", username);
        public Task<DataAccessResult> SetStudentInfoAsync(StudentInfo info) => SetResourceAsync(info);

        public Task<DataAccessResult<Schedule>> GetScheduleAsync(string username) => GetResourceAsync<Schedule>($"Schedule-{username}", username);
        public Task<DataAccessResult> SetScheduleAsync(Schedule schedule) => SetResourceAsync(schedule);

        public Task<DataAccessResult<ExamSchedule>> GetExamsAsync(string username) => GetResourceAsync<ExamSchedule>($"Exams-{username}", username);
        public Task<DataAccessResult> SetExamsAsync(ExamSchedule exams) => SetResourceAsync(exams);

        public Task<DataAccessResult<ScoreSet>> GetScoreAsync(string username, bool isSecondMajor) => GetResourceAsync<ScoreSet>($"Score-{username}-{(isSecondMajor ? "S" : "M")}", username);
        public Task<DataAccessResult> SetScoreAsync(ScoreSet scoreSet) => SetResourceAsync(scoreSet);

        public Task<DataAccessResult<UserPreferences>> GetUserPreferences(string username) => GetResourceAsync<UserPreferences>($"Preferences-{username}", username);
        public Task<DataAccessResult> SetUserPreferences(UserPreferences preferences) => SetResourceAsync(preferences);

        public async Task<DataAccessResult> DeleteUserAsync(string username)
        {
            List<Task> deleteTasks = new List<Task>(5);
            // For some reason, sharing partition key would cause a compiler error.
            // See https://github.com/dotnet/roslyn/issues/47304
            deleteTasks.Add(container.DeleteItemAsync<object>($"Student-{username}", new PartitionKey(username)));
            deleteTasks.Add(container.DeleteItemAsync<object>($"Schedule-{username}", new PartitionKey(username)));
            deleteTasks.Add(container.DeleteItemAsync<object>($"Exams-{username}", new PartitionKey(username)));
            deleteTasks.Add(container.DeleteItemAsync<object>($"Score-{username}-M", new PartitionKey(username)));
            deleteTasks.Add(container.DeleteItemAsync<object>($"Score-{username}-S", new PartitionKey(username)));
            deleteTasks.Add(container.DeleteItemAsync<object>($"PushChannels-Wns-{username}", new PartitionKey(username)));
            deleteTasks.Add(container.DeleteItemAsync<object>($"Preferences-{username}", new PartitionKey(username)));
            bool hasError = false;
            try
            {
                await Task.WhenAll(deleteTasks);
            }
            catch (AggregateException aggregateException)
            {
                aggregateException.Handle(ex =>
                {
                    if (!(ex is CosmosException cosmosEx && cosmosEx.Status == 404))
                    {
                        hasError = true;
                    }
                    return true;
                });
            }
            catch (CosmosException ex)
            {
                if (ex.Status != 404)
                {
                    hasError = true;
                }
            }
            catch (Exception)
            {
                hasError = true;
            }

            if (hasError)
            {
                return new DataAccessResult(false, -1);
            }
            try
            {
                await container.DeleteItemAsync<object>($"Credential-{username}", new PartitionKey("Credential"));
            }
            catch (CosmosException ex)
            {
                if (ex.Status != 404)
                {
                    return new DataAccessResult(false, -1);
                }
            }
            catch (Exception)
            {
                return new DataAccessResult(false, -1);
            }
            return DataAccessResult.Ok;
        }

        public Task<DataAccessResult<DeveloperMessage>> GetDeveloperMessageAsync() => GetResourceAsync<DeveloperMessage>("DevMessage", "DevMessage");

        public Task<DataAccessResult<PushAccessToken>> GetPushAccessTokenAsync(PushPlatform platform) => GetResourceAsync<PushAccessToken>($"{platform}PushToken", $"{platform}PushToken");
        public Task<DataAccessResult> SetPushAccessTokenAsync(PushAccessToken token) => SetResourceAsync<PushAccessToken>(token);

        public Task<DataAccessResult<NotificationChannelCollection>> GetPushChannelsAsync(string username, PushPlatform platform)
        {
            string id = $"PushChannels-{platform}-{username}";
            return GetResourceAsync<NotificationChannelCollection>(id, username);
        }
        public async Task<DataAccessResult> AddPushChannelAsync(string username, PushPlatform platform, string channelIdentifier)
        {
            DataAccessResult<NotificationChannelCollection> fetchResult = await GetPushChannelsAsync(username, platform);
            NotificationChannelCollection collection;
            if (fetchResult.Success)
            {
                collection = fetchResult.Resource;
            }
            else if (fetchResult.StatusCode == 404)
            {
                collection = new NotificationChannelCollection(username, platform);
            }
            else
            {
                return new DataAccessResult(false, fetchResult.StatusCode);
            }
            if (collection.Channels.Exists(x => x.ChannelIdentifier.Equals(channelIdentifier, StringComparison.Ordinal)))
            {
                return DataAccessResult.Ok;
            }

            collection.Channels.Add(new NotificationChannelItem(channelIdentifier));
            if (collection.Channels.Count > maxChannelCountPerPlatform)
            {
                collection.Channels.RemoveAt(0);
            }
            return await SetResourceAsync(collection);
        }
        public async Task<DataAccessResult> RemovePushChannelAsync(string username, PushPlatform platform, IEnumerable<string> channelIdentifiers)
        {
            DataAccessResult<NotificationChannelCollection> fetchResult = await GetPushChannelsAsync(username, platform);
            NotificationChannelCollection collection;
            if (fetchResult.Success)
            {
                collection = fetchResult.Resource;
            }
            else
            {
                return new DataAccessResult(false, fetchResult.StatusCode);
            }

            int removedCount = 0;
            foreach (string id in channelIdentifiers)
            {
                removedCount += collection.Channels.RemoveAll(x => x.ChannelIdentifier.Equals(id, StringComparison.Ordinal));
            }
            if (removedCount == 0)
            {
                return new DataAccessResult(false, 404);
            }
            return await SetResourceAsync(collection);
        }

        private async Task<DataAccessResult<T>> GetResourceAsync<T>(string id, string partitionKey) where T : ICosmosResource
        {
            try
            {
                ItemResponse<CosmosResource<T>> result = await container.ReadItemAsync<CosmosResource<T>>(id, new PartitionKey(partitionKey));
                return DataAccessResult<T>.Ok(result.Value.Resource);
            }
            catch (CosmosException ex)
            {
                return new DataAccessResult<T>(false, default(T), ex.Status);
            }
        }
        private async Task<DataAccessResult> SetResourceAsync<T>(T resource) where T : ICosmosResource
        {
            try
            {
                var res = new CosmosResource<T>(resource);
                ItemResponse<CosmosResource<T>> result = await container.UpsertItemAsync<CosmosResource<T>>(res, new PartitionKey(res.Pk));
                return DataAccessResult.Ok;
            }
            catch (CosmosException ex)
            {
                return new DataAccessResult(false, ex.Status);
            }
        }

        private readonly CosmosContainer container;
        private readonly ICredentialEncryptionService encryptionService;
        private readonly int maxChannelCountPerPlatform;
    }
}
