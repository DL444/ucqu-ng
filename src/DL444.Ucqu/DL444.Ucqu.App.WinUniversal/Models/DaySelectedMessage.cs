using System;
using DL444.Ucqu.App.WinUniversal.Services;

namespace DL444.Ucqu.App.WinUniversal.Models
{
    internal struct DaySelectedMessage : IMessage
    {
        public DaySelectedMessage(DateTimeOffset selectedDate) => SelectedDate = selectedDate;
        public DateTimeOffset SelectedDate { get; set; }
    }
}
