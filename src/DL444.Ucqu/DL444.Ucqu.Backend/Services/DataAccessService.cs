using System.Threading.Tasks;
using Azure.Cosmos;
using DL444.Ucqu.Models;

namespace DL444.Ucqu.Backend.Services
{
    public interface IDataAccessService
    {
        Task<DataAccessResult<StudentCredential>> GetCredentialAsync(string username);
        Task<DataAccessResult> SetCredentialAsync(StudentCredential credential);
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
            try
            {
                ItemResponse<CosmosResource<StudentCredential>> result = await container.ReadItemAsync<CosmosResource<StudentCredential>>($"Credential-{username}", new PartitionKey(username));
                StudentCredential credential = result.Value.Resource;
                encryptionService.DecryptCredential(credential);
                return DataAccessResult<StudentCredential>.Ok(credential);
            }
            catch (CosmosException ex)
            {
                return new DataAccessResult<StudentCredential>(false, null, ex.Status);
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                // Key changed after last encryption.
                return new DataAccessResult<StudentCredential>(false, null, 1);
            }
        }
        public async Task<DataAccessResult> SetCredentialAsync(StudentCredential credential)
        {
            try
            {
                encryptionService.EncryptCredential(credential);
                var resource = new CosmosResource<StudentCredential>(credential);
                ItemResponse<CosmosResource<StudentCredential>> result = await container.UpsertItemAsync<CosmosResource<StudentCredential>>(resource, new PartitionKey(resource.Pk));
                return DataAccessResult.Ok;
            }
            catch (CosmosException ex)
            {
                return new DataAccessResult(false, ex.Status);
            }
        }
        public async Task<DataAccessResult<ScoreSet>> GetScoreAsync(string username, bool isSecondMajor)
        {
            try
            {
                ItemResponse<CosmosResource<ScoreSet>> result = await container.ReadItemAsync<CosmosResource<ScoreSet>>($"Score-{username}-{(isSecondMajor ? "S" : "M")}", new PartitionKey(username));
                return DataAccessResult<ScoreSet>.Ok(result.Value.Resource);
            }
            catch (CosmosException ex)
            {
                return new DataAccessResult<ScoreSet>(false, null, ex.Status);
            }
        }
        public async Task<DataAccessResult> SetScoreAsync(ScoreSet scoreSet)
        {
            try
            {
                var resource = new CosmosResource<ScoreSet>(scoreSet);
                ItemResponse<CosmosResource<ScoreSet>> result = await container.UpsertItemAsync<CosmosResource<ScoreSet>>(resource, new PartitionKey(resource.Pk));
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
