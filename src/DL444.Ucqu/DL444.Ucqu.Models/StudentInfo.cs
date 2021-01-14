using System;
using System.Text.Json.Serialization;

namespace DL444.Ucqu.Models
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
        public string PasswordHash { get; set; }
        public string Iv { get; set; }
    }
}
