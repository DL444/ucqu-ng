using System.Text.Json.Serialization;

namespace DL444.Ucqu.Models
{
    public class StudentInfo
    {
        [JsonPropertyName("id")]
        public string Id => $"Student-{StudentId}";
        [JsonPropertyName("pk")]
        public string Pk => StudentId;
        public RecordStatus RecordStatus { get; set; }

        public string StudentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Major { get; set; } = string.Empty;
        public int Class { get; set; }
        public string? SecondMajor { get; set; }

    }
}
