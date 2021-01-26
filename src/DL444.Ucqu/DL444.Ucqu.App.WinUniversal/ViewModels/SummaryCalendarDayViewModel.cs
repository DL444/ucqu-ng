using System;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace DL444.Ucqu.App.WinUniversal.ViewModels
{
    internal class SummaryCalendarDayViewModel : INotifyPropertyChanged
    {
        public SummaryCalendarDayViewModel(DateTimeOffset day)
        {
            Date = day;
            Key = day.ToString("s");
        }

        public string Key { get; }
        public DateTimeOffset Date { get; }

        public Brush Slot0Brush => GetSlotBrush(0);
        public Brush Slot1Brush => GetSlotBrush(1);
        public Brush Slot2Brush => GetSlotBrush(2);
        public Brush Slot3Brush => GetSlotBrush(3);
        public Brush Slot4Brush => GetSlotBrush(4);

        public event PropertyChangedEventHandler PropertyChanged;

        public void BeginChanges()
        {
            if (oldSlots == null)
            {
                oldSlots = new SummaryCalendarSlotStatus[5];
                for (int i = 0; i < 5; i++)
                {
                    oldSlots[i] = slots[i];
                    slots[i] = SummaryCalendarSlotStatus.Free;
                }
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    slots[i] = SummaryCalendarSlotStatus.Free;
                }
            }
        }

        public void AddSchedule(SummaryCalendarSlotStatus status, int startSession, int endSession)
        {
            int startSlot = GetSlotForSession(startSession);
            int endSlot = GetSlotForSession(endSession);
            for (int i = startSlot; i <= endSlot; i++)
            {
                if (slots[i] == SummaryCalendarSlotStatus.Free)
                {
                    slots[i] = status;
                }
                else if (slots[i] == SummaryCalendarSlotStatus.Course && status == SummaryCalendarSlotStatus.Exam)
                {
                    slots[i] = status;
                }
            }
        }

        public void CommitChanges()
        {
            if (oldSlots == null)
            {
                throw new InvalidOperationException("Call BeginChanges before making or commiting changes.");
            }
            for (int i = 0; i < 5; i++)
            {
                if (slots[i] != oldSlots[i])
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"Slot{i}Brush"));
                }
            }
            oldSlots = null;
        }

        private Brush GetSlotBrush(int index)
        {
            string resourceName;
            if (slots[index] == SummaryCalendarSlotStatus.Free)
            {
                resourceName = "SummaryCalendarFreeSlotBrush";
            }
            else if (slots[index] == SummaryCalendarSlotStatus.Course)
            {
                resourceName = "SummaryCalendarCourseSlotBrush";
            }
            else if (slots[index] == SummaryCalendarSlotStatus.Exam)
            {
                resourceName = "SummaryCalendarExamSlotBrush";
            }
            else
            {
                throw new NotSupportedException("Unexpected slot status value.");
            }
            return (Brush)Application.Current.Resources[resourceName];
        }

        private int GetSlotForSession(int session)
        {
            session = Math.Max(0, session);
            session = Math.Min(sessionSlotRemap.Length - 1, session);
            return sessionSlotRemap[session - 1];
        }

        private SummaryCalendarSlotStatus[] slots = new SummaryCalendarSlotStatus[5];
        private SummaryCalendarSlotStatus[] oldSlots;

        private static int[] sessionSlotRemap = new int[] { 0, 0, 1, 1, 2, 2, 2, 3, 3, 4, 4, 4 };
    }

    internal enum SummaryCalendarSlotStatus
    {
        Free,
        Course,
        Exam
    }
}
