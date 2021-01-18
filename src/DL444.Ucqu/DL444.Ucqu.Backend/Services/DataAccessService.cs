using System.Threading.Tasks;
using Azure.Cosmos;
using DL444.Ucqu.Models;

namespace DL444.Ucqu.Backend.Services
{
    public interface IDataAccessService
    {
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
    }

    internal class DataAccessService : IDataAccessService
    {
        public DataAccessService(CosmosClient dbClient, string databaseId, string containerId, ICredentialEncryptionService encryptionService)
        {
            container = dbClient.GetContainer(databaseId, containerId);
            this.encryptionService = encryptionService;
        }

        public async Task<DataAccessResult<StudentCredential>> GetCredentialAsync(string username)
        {
            DataAccessResult<StudentCredential> fetchResult = await GetResource<StudentCredential>($"Credential-{username}", username);
            if (fetchResult.Success)
            {
                try
                {
                    encryptionService.DecryptCredential(fetchResult.Resource);
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    // Key changed after last encryption.
                    return new DataAccessResult<StudentCredential>(false, null, 1);
                }
            }
            return fetchResult;
        }
        public async Task<DataAccessResult> SetCredentialAsync(StudentCredential credential)
        {
            encryptionService.EncryptCredential(credential);
            return await SetResource(credential);
        }

        public async Task<DataAccessResult<UserInitializeStatus>> GetUserInitializeStatusAsync(string id) => await GetResource<UserInitializeStatus>($"UserInitStatus-{id}", "UserInitStatus");
        public async Task<DataAccessResult> SetUserInitializeStatusAsync(UserInitializeStatus status) => await SetResource(status);
        public async Task<DataAccessResult> PurgeUserInitializeStatusAsync()
        {
            long threshold = System.DateTimeOffset.UtcNow.AddMinutes(-15).ToUnixTimeMilliseconds();
            System.Collections.Generic.List<string> ids = new System.Collections.Generic.List<string>();
            QueryDefinition query = new QueryDefinition($"SELECT c.id FROM c WHERE c.pk = \"UserInitStatus\" AND c.Resource.LastUpdateTimestamp < {threshold}");
            await foreach (Models.IdHeader header in container.GetItemQueryIterator<Models.IdHeader>(query, requestOptions: new QueryRequestOptions()
            {
                PartitionKey = new PartitionKey("UserInitStatus"),
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

        public async Task<DataAccessResult<StudentInfo>> GetStudentInfoAsync(string username) => await GetResource<StudentInfo>($"Student-{username}", username);
        public async Task<DataAccessResult> SetStudentInfoAsync(StudentInfo info) => await SetResource(info);

        public async Task<DataAccessResult<Schedule>> GetScheduleAsync(string username) => await GetResource<Schedule>($"Schedule-{username}", username);
        public async Task<DataAccessResult> SetScheduleAsync(Schedule schedule) => await SetResource(schedule);

        public async Task<DataAccessResult<ExamSchedule>> GetExamsAsync(string username) => await GetResource<ExamSchedule>($"Exams-{username}", username);
        public async Task<DataAccessResult> SetExamsAsync(ExamSchedule exams) => await SetResource(exams);

        public async Task<DataAccessResult<ScoreSet>> GetScoreAsync(string username, bool isSecondMajor)
            => await GetResource<ScoreSet>($"Score-{username}-{(isSecondMajor ? "S" : "M")}", username);
        public async Task<DataAccessResult> SetScoreAsync(ScoreSet scoreSet) => await SetResource(scoreSet);

        private async Task<DataAccessResult<T>> GetResource<T>(string id, string partitionKey) where T : ICosmosResource
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
        private async Task<DataAccessResult> SetResource<T>(T resource) where T : ICosmosResource
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

        private CosmosContainer container;
        private ICredentialEncryptionService encryptionService;
    }
}
