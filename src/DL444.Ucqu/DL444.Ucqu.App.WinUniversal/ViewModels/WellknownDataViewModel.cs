using System;
using System.Collections.Generic;
using DL444.Ucqu.Models;

namespace DL444.Ucqu.App.WinUniversal.ViewModels
{
    public struct WellknownDataViewModel
    {
        public WellknownDataViewModel(WellknownData data)
        {
            CurrentTerm = data.CurrentTerm;
            TermStartDate = data.TermStartDate;
            TermEndDate = data.TermEndDate.AddDays(-1);
            Schedule = data.Schedule;
            Model = data;
        }

        public string CurrentTerm { get; }
        public DateTimeOffset TermStartDate { get; }
        public DateTimeOffset TermEndDate { get; }
        public List<ScheduleTime> Schedule { get; }
        public WellknownData Model { get; }
    }
}
