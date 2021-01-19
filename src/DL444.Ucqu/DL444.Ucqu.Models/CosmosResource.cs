using System.Text.Json.Serialization;

namespace DL444.Ucqu.Models
{
    public class CosmosResource<T> where T : ICosmosResource
    {
        public CosmosResource(T resource) => Resource = resource;

        [JsonPropertyName("id")]
        public string Id => Resource.Id;
        [JsonPropertyName("pk")]
        public string Pk => Resource.PartitionKey;
        public T Resource { get; set; }
    }

    public interface ICosmosResource
    {
        string Id { get; }
        string PartitionKey { get; }
    }

    public interface IStatusResource : ICosmosResource
    {
        RecordStatus RecordStatus { get; }
    }
}