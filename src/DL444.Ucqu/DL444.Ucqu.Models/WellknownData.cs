using System;

namespace DL444.Ucqu.Models
{
    public struct WellknownData
    {
        public string CurrentTerm { get; set; }
        public DateTimeOffset TermStartDate { get; set; }
        public DateTimeOffset TermEndDate { get; set; }
    }
}
