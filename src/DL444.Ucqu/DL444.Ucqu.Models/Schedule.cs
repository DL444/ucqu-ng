using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace DL444.Ucqu.Models
{
    public class Schedule
    {
        public Schedule(string studentId) => StudentId = studentId;

        [JsonPropertyName("id")]
        public string Id => $"Schedule-{StudentId}";
        [JsonPropertyName("pk")]
        public string Pk => StudentId;
        public RecordStatus RecordStatus { get; set; }

        public string StudentId { get; set; }
        public SortedList<int, ScheduleWeek> Weeks { get; set; } = new SortedList<int, ScheduleWeek>();

        public void AddEntry(int week, ScheduleEntry entry)
        {
            if (!Weeks.ContainsKey(week))
            {
                Weeks.Add(week, new ScheduleWeek(week));
            }
            Weeks[week].Entries.Add(entry);
        }

        public List<ScheduleEntry> GetDaySchedule(int day)
        {
            int weekNumber = day / 7 + 1;
            if (!Weeks.ContainsKey(weekNumber))
            {
                return new List<ScheduleEntry>();
            }
            ScheduleWeek week = Weeks[weekNumber];
            int dayOfWeek = day % 7 + 1;
            return week.Entries.Where(x => x.DayOfWeek == dayOfWeek).OrderBy(x => x.StartSession).ToList();
        }
    }

    public class ScheduleWeek
    {
        public ScheduleWeek(int weekNumber) => WeekNumber = weekNumber;

        public int WeekNumber { get; set; }
        public List<ScheduleEntry> Entries { get; set; } = new List<ScheduleEntry>();
    }

    public class ScheduleEntry
    {
        public ScheduleEntry(string name, string lecturer, string room)
        {
            Name = name;
            Lecturer = lecturer;
            Room = room;
        }

        public string Name { get; set; }
        public string Lecturer { get; set; }
        public string Room { get; set; }
        public int DayOfWeek { get; set; }
        public int StartSession { get; set; }
        public int EndSession { get; set; }
    }
}
