using System;
using System.Text.Json.Serialization;

namespace DL444.Ucqu.Library.Models
{
    public class StudentInfo
    {
        [JsonPropertyName("id")]
        public string Id => $"Student-{StudentId}";
        [JsonPropertyName("pk")]
        public string Pk => Id;

        public string StudentId { get; set; }
        public string Name { get; set; }
        public string Year { get; set; }
        public string Major { get; set; }
        public string Class { get; set; }
    }
}
