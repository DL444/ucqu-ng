﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DL444.Ucqu.Library.Models
{
    public class WellknownData
    {
        [JsonPropertyName("id")]
        public string Id => "Wellknown";
        [JsonPropertyName("pk")]
        public string Pk => "Wellknown";

        public string CurrentTerm { get; set; }
        public DateTimeOffset TermStartDate { get; set; }
        public List<ScheduleTime> Schedule { get; set; }
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
