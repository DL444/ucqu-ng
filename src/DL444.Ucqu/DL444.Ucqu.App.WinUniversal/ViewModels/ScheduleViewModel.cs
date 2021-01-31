using System;
using System.Collections.Generic;
using System.Linq;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Services;
using DL444.Ucqu.Models;
using Windows.UI.Xaml;

namespace DL444.Ucqu.App.WinUniversal.ViewModels
{
    public struct ScheduleViewModel
    {
        public ScheduleViewModel(Schedule schedule, WellknownData wellknown)
        {
            Weeks = schedule.Weeks.OrderBy(x => x.WeekNumber).Select(x => new ScheduleWeekViewModel(x, wellknown)).ToList();
            if (Weeks.Count > 1)
            {
                for (int i = 0; i < Weeks.Count - 1; i++)
                {
                    int insertCount = Weeks[i + 1].WeekNumber - Weeks[i].WeekNumber - 1;
                    for (int j = 0; j < insertCount; j++)
                    {
                        ScheduleWeekViewModel emptyWeek = new ScheduleWeekViewModel(new ScheduleWeek(Weeks[i].WeekNumber + j + 1), wellknown);
                        Weeks.Insert(i + j + 1, emptyWeek);
                    }
                    i += insertCount;
                }
            }
            int daysSinceTermStart = (int)(DateTimeOffset.Now.Date - wellknown.TermStartDate).TotalDays;
            int weekNumber = daysSinceTermStart / 7;
            if (weekNumber < 0 || weekNumber >= Weeks.Count)
            {
                Today = new List<ScheduleEntryViewModel>();
            }
            else
            {
                int todayDayOfWeek = (int)DateTimeOffset.Now.DayOfWeek;
                todayDayOfWeek = todayDayOfWeek == 0 ? 7 : todayDayOfWeek;
                ScheduleDayViewModel day = Weeks[weekNumber].Days[todayDayOfWeek];
                Today = day.Entries.Where(x => x.LocalEndTime > DateTimeOffset.Now).ToList() ?? new List<ScheduleEntryViewModel>();
            }
        }

        public List<ScheduleWeekViewModel> Weeks { get; }
        public List<ScheduleEntryViewModel> Today { get; }
        public bool IsTodayOccupied => Today != null && Today.Count > 0;
    }

    public struct ScheduleWeekViewModel
    {
        public ScheduleWeekViewModel(ScheduleWeek week, WellknownData wellknown)
        {
            WeekNumber = week.WeekNumber;
            WeekNumberDisplay = Application.Current.GetService<ILocalizationService>().Format("ScheduleTableWeekNumberHeaderFormat", week.WeekNumber);
            Days = new ScheduleDayViewModel[7];
            DateTimeOffset weekStartDate = wellknown.TermStartDate.GetLocalDate().AddDays((week.WeekNumber - 1) * 7);
            foreach(var group in week.Entries.GroupBy(x => x.DayOfWeek))
            {
                int dayOfWeek = group.Key;
                DateTimeOffset day = weekStartDate.AddDays(dayOfWeek - 1);
                Days[dayOfWeek - 1] = new ScheduleDayViewModel(WeekNumber, day, group, wellknown);
            }
            for (int i = 0; i < 7; i++)
            {
                if (!Days[i].Initialized)
                {
                    DateTimeOffset day = weekStartDate.AddDays(i);
                    Days[i] = new ScheduleDayViewModel(WeekNumber, day, Array.Empty<ScheduleEntry>(), wellknown);
                }
            }
        }

        public int WeekNumber { get; }
        public string WeekNumberDisplay { get; }
        public ScheduleDayViewModel[] Days { get; }

        public ScheduleDayViewModel Day0 => Days[0];
        public ScheduleDayViewModel Day1 => Days[1];
        public ScheduleDayViewModel Day2 => Days[2];
        public ScheduleDayViewModel Day3 => Days[3];
        public ScheduleDayViewModel Day4 => Days[4];
        public ScheduleDayViewModel Day5 => Days[5];
        public ScheduleDayViewModel Day6 => Days[6];
    }

    public struct ScheduleDayViewModel
    {
        public ScheduleDayViewModel(int weekNumber, DateTimeOffset date, IEnumerable<ScheduleEntry> entries, WellknownData schedule)
        {
            ILocalizationService locService = Application.Current.GetService<ILocalizationService>();
            LocalDate = date;
            LocalDateDisplay = locService.Format("ScheduleTableDayDisplayFormat", date);
            WeekNumber = weekNumber;
            DayOfWeek = date.DayOfWeek == System.DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
            DayOfWeekDisplay = locService.GetString($"ScheduleTableDayOfWeek{DayOfWeek}Header");
            IsToday = date == DateTimeOffset.Now.GetLocalDate();
            Entries = entries.OrderBy(x => x.StartSession).Select(x => new ScheduleEntryViewModel(x, date, schedule)).ToList();
            Initialized = true;
        }

        public DateTimeOffset LocalDate { get; }
        public string LocalDateDisplay { get; }
        public int WeekNumber { get; }
        public bool BackgroundVisibility => (WeekNumber + DayOfWeek) % 2 != 0;
        public int DayOfWeek { get; }
        public string DayOfWeekDisplay { get; }
        public bool IsToday { get; }
        public List<ScheduleEntryViewModel> Entries { get; }
        public bool Initialized { get; }
    }

    public struct ScheduleEntryViewModel
    {
        public ScheduleEntryViewModel(ScheduleEntry entry, DateTimeOffset date, WellknownData schedule)
        {
            Name = entry.Name;
            Lecturer = entry.Lecturer;
            Room = entry.Room;
            DayOfWeek = entry.DayOfWeek;
            StartSession = Math.Min(entry.StartSession, schedule.Schedule.Count);
            EndSession = Math.Min(entry.EndSession, schedule.Schedule.Count);

            TimeSpan startTime = schedule.Schedule[StartSession - 1].StartOffset;
            TimeSpan endTime = schedule.Schedule[EndSession - 1].EndOffset;
            LocalStartTime = date.Add(startTime);
            LocalEndTime = date.Add(endTime);
            ILocalizationService locService = Application.Current.GetService<ILocalizationService>();
            TimeRangeDisplay = locService.Format("ScheduleSummaryTimeRangeFormat", LocalStartTime.TimeOfDay, LocalEndTime.TimeOfDay);
            TimeRangeRoomDisplay = locService.Format("ScheduleSummaryTimeRangeRoomFormat", LocalStartTime.TimeOfDay, LocalEndTime.TimeOfDay, Room);
        }

        public string Name { get; }
        public string Lecturer { get; }
        public string Room { get; }
        public int DayOfWeek { get; }
        public int StartSession { get; }
        public int EndSession { get; }
        public DateTimeOffset LocalStartTime { get; }
        public DateTimeOffset LocalEndTime { get; }
        public string TimeRangeDisplay { get; }
        public string TimeRangeRoomDisplay { get; }
    }
}
