using System;
using System.Collections.Generic;

namespace DL444.Ucqu.Models
{
    public struct WellknownData
    {
        public string CurrentTerm { get; set; }
        public DateTimeOffset TermStartDate { get; set; }
        public DateTimeOffset TermEndDate { get; set; }
        public List<ScheduleTime> Schedule { get; set; }
    }
}
