using System;
using System.Collections.Generic;
using System.Linq;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Services;
using DL444.Ucqu.Models;
using Windows.UI.Xaml;

namespace DL444.Ucqu.App.WinUniversal.ViewModels
{
    public struct ExamScheduleViewModel
    {
        public ExamScheduleViewModel(ExamSchedule exams, WellknownData schedule)
        {
            Exams = exams.Exams.Select(x => new ExamViewModel(x, schedule)).ToList();
            int recentExamsThreshold = Application.Current.GetConfigurationValue("RecentExamsThreshold", 15);
            RecentExams = Exams.Where(x => x.EndTime > DateTimeOffset.Now && x.StartTime < DateTimeOffset.Now.AddDays(recentExamsThreshold)).OrderBy(x => x.Countdown).ToList();
        }

        public List<ExamViewModel> Exams { get; }
        public List<ExamViewModel> RecentExams { get; }
        public bool HasRecentExams => RecentExams != null && RecentExams.Count > 0;
    }

    public struct ExamViewModel
    {
        public ExamViewModel(Exam exam, WellknownData schedule)
        {
            ShortName = exam.ShortName;
            StartTime = exam.StartTime.LocalDateTime;
            EndTime = exam.EndTime.LocalDateTime;
            Countdown = (int)(StartTime.GetLocalDate() - DateTimeOffset.Now.Date).TotalDays;
            EquivalentStartSession = FindEqivalentSession(StartTime.TimeOfDay, schedule);
            EquivalentEndSession = FindEqivalentSession(EndTime.TimeOfDay, schedule);
            Week = exam.Week;
            DayOfWeek = exam.DayOfWeek;
            ShortLocation = exam.ShortLocation;
            Seating = exam.Seating;

            ILocalizationService locService = Application.Current.GetService<ILocalizationService>();
            TimeRangeDisplay = locService.Format("ScheduleSummaryTimeRangeFormat", StartTime.ToLocalTime().TimeOfDay, EndTime.ToLocalTime().TimeOfDay);
            if (Countdown < 0)
            {
                CountdownDisplay = locService.GetString("ScheduleSummaryExamCountdownEnded");
            }
            else if (Countdown < 3)
            {
                CountdownDisplay = locService.GetString($"ScheduleSummaryExamCountdown{Countdown}");
            }
            else
            {
                CountdownDisplay = locService.Format("ScheduleSummaryExamCountdownFormat", Countdown);
            }
        }

        public string ShortName { get; }
        public DateTimeOffset StartTime { get; }
        public DateTimeOffset EndTime { get; }
        public int Countdown { get; }
        public string CountdownDisplay { get; }
        public string TimeRangeDisplay { get; }
        public int EquivalentStartSession { get; }
        public int EquivalentEndSession { get; }
        public int Week { get; }
        public int DayOfWeek { get; }
        public string ShortLocation { get; }
        public int Seating { get; }

        private static int FindEqivalentSession(TimeSpan time, WellknownData schedule)
        {
            if (schedule.Schedule == null)
            {
                return 0;
            }
            for (int i = 0; i < schedule.Schedule.Count; i++)
            {
                if (time <= schedule.Schedule[i].StartOffset)
                {
                    return i;
                }
            }
            return schedule.Schedule.Count;
        }
    }
}
