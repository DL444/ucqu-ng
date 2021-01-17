using System.Threading.Tasks;
using Azure.Cosmos;
using DL444.Ucqu.Models;

namespace DL444.Ucqu.Backend.Services
{
    public interface IDataAccessService
    {
        Task<DataAccessResult<StudentCredential>> GetCredentialAsync(string username);
        Task<DataAccessResult> SetCredentialAsync(StudentCredential credential);
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
                ItemResponse<CosmosResource<StudentCredential>> result = await container.UpsertItemAsync<CosmosResource<StudentCredential>>(resource, new PartitionKey(credential.StudentId));
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
