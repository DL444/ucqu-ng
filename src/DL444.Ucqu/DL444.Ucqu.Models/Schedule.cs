using System.Collections.Generic;
using System.Linq;

namespace DL444.Ucqu.Models
{
    public class Schedule : IStatusResource, ICosmosResource
    {
        public Schedule() { }
        public Schedule(string studentId) => StudentId = studentId;

        public string Id() => $"Schedule-{StudentId}";
        public string PartitionKey() => StudentId;
        public RecordStatus RecordStatus { get; set; }

        public string StudentId { get; set; }
        public List<ScheduleWeek> Weeks { get; set; } = new List<ScheduleWeek>();

        public void AddEntry(int week, ScheduleEntry entry)
        {
            ScheduleWeek scheduleWeek = Weeks.FirstOrDefault(x => x.WeekNumber == week);
            if (scheduleWeek == null)
            {
                scheduleWeek = new ScheduleWeek(week);
                Weeks.Add(scheduleWeek);
            }
            scheduleWeek.Entries.Add(entry);
        }
    }

    public class ScheduleWeek
    {
        public ScheduleWeek() { }
        public ScheduleWeek(int weekNumber) => WeekNumber = weekNumber;

        public int WeekNumber { get; set; }
        public List<ScheduleEntry> Entries { get; set; } = new List<ScheduleEntry>();
    }

    public class ScheduleEntry
    {
        public ScheduleEntry() { }
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
