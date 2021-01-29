using System.Text.Json.Serialization;
using DL444.Ucqu.Backend.Services;
using DL444.Ucqu.Models;

namespace DL444.Ucqu.Backend.Models
{
    public struct PushAccessToken : ICosmosResource
    {
        public string Id() => $"{Platform}PushToken";
        public string PartitionKey() => $"{Platform}PushToken";

        public PushPlatform Platform { get; set; }
        [JsonPropertyName("access_token")]
        public string Token { get; set; }
    }
}
