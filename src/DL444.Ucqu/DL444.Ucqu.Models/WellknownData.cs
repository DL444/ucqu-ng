using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DL444.Ucqu.Models
{
    public class WellknownData : ICosmosResource
    {
        [JsonIgnore]
        public string Id => "Wellknown";
        [JsonIgnore]
        public string PartitionKey => "Wellknown";

        public string CurrentTerm { get; set; } = string.Empty;
        public DateTimeOffset TermStartDate { get; set; }
        public List<ScheduleTime> Schedule { get; set; } = new List<ScheduleTime>();
    }

    public struct ScheduleTime
    {
        public ScheduleTime(TimeSpan startOffset, TimeSpan endOffset)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
        }

        public TimeSpan StartOffset { get; set; }
        public TimeSpan EndOffset { get; set; }
        public TimeSpan Duration => EndOffset - StartOffset;
    }
}
