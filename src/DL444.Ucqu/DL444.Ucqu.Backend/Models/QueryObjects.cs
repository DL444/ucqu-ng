using System.Text.Json.Serialization;

namespace DL444.Ucqu.Backend.Models
{
    internal struct IdHeader
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

    internal struct CountHeader
    {
        public int ItemCount { get; set; }
    }

    internal struct StudentIdHeader
    {
        public string StudentId { get; set; }
    }
}
