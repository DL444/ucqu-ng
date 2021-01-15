using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DL444.Ucqu.Models
{
    public class ExamSchedule
    {
        public string StudentId { get; set; }
        public List<Exam> Exams { get; set; } = new List<Exam>();
    }
    
    public class Exam
    {
        public string Name { get; set; }
        [JsonInclude]
        public string ShortName => Utilities.GetShortformName(Name);
        public double Credit { get; set; }
        public string Category { get; set; }
        public string Type { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public int Week { get; set; }
        public int DayOfWeek { get; set; }
        public string Location { get; set; }
        public int Seating { get; set; }
    }
}
