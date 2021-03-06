﻿using System;
using System.Collections.Generic;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Models;
using DL444.Ucqu.App.WinUniversal.Services;
using DL444.Ucqu.App.WinUniversal.ViewModels;
using DL444.Ucqu.Models;
using Microsoft.AppCenter.Analytics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DL444.Ucqu.App.WinUniversal.Controls
{
    public sealed partial class ScheduleSummary : UserControl
    {
        public ScheduleSummary()
        {
            TermRange = new WellknownDataViewModel(new WellknownData()
            {
                TermStartDate = DateTimeOffset.Now.Date,
                TermEndDate = DateTimeOffset.Now.Date.AddDays(1)
            });
            this.InitializeComponent();
        }

        public WellknownDataViewModel TermRange
        {
            get => (WellknownDataViewModel)GetValue(TermRangeProperty);
            set => SetValue(TermRangeProperty, value);
        }

        public static readonly DependencyProperty TermRangeProperty =
            DependencyProperty.Register(nameof(TermRange), typeof(WellknownDataViewModel), typeof(ScheduleSummary), null);

        public ExamScheduleViewModel Exams
        {
            get => (ExamScheduleViewModel)GetValue(ExamsProperty);
            set => SetValue(ExamsProperty, value);
        }

        public static readonly DependencyProperty ExamsProperty =
            DependencyProperty.Register(nameof(Exams), typeof(ExamScheduleViewModel), typeof(ScheduleSummary), new PropertyMetadata(new ExamScheduleViewModel(), OnScheduleChanged));

        public ScheduleViewModel Schedule
        {
            get => (ScheduleViewModel)GetValue(ScheduleProperty);
            set => SetValue(ScheduleProperty, value);
        }

        public static readonly DependencyProperty ScheduleProperty =
            DependencyProperty.Register(nameof(Schedule), typeof(ScheduleViewModel), typeof(ScheduleSummary), new PropertyMetadata(new ScheduleViewModel(), OnScheduleChanged));

        public bool IsTodayFree
        {
            get => (bool)GetValue(IsTodayFreeProperty);
            private set => SetValue(IsTodayFreeProperty, value);
        }

        public static readonly DependencyProperty IsTodayFreeProperty =
            DependencyProperty.Register(nameof(IsTodayFree), typeof(bool), typeof(ScheduleSummary), new PropertyMetadata(false));

        public bool IsOnVacation
        {
            get => (bool)GetValue(IsOnVacationProperty);
            private set => SetValue(IsOnVacationProperty, value);
        }

        public static readonly DependencyProperty IsOnVacationProperty =
            DependencyProperty.Register(nameof(IsOnVacation), typeof(bool), typeof(ScheduleSummary), new PropertyMetadata(false));

        private static void OnScheduleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is ScheduleSummary summary))
            {
                return;
            }

            foreach (var day in summary.days.Values)
            {
                day.BeginChanges();
            }

            ScheduleViewModel schedule = summary.Schedule;
            if (schedule.Weeks != null)
            {
                foreach (ScheduleWeekViewModel week in schedule.Weeks)
                {
                    foreach (ScheduleDayViewModel day in week.Days)
                    {
                        if (day.Entries == null)
                        {
                            continue;
                        }
                        foreach (ScheduleEntryViewModel entry in day.Entries)
                        {
                            summary.AddScheduleToCalendar(entry.LocalStartTime, SummaryCalendarSlotStatus.Course, entry.StartSession, entry.EndSession);
                        }
                    }
                }
            }

            ExamScheduleViewModel exams = summary.Exams;
            if (exams.Exams != null)
            {
                foreach (var exam in exams.Exams)
                {
                    summary.AddScheduleToCalendar(exam.StartTime, SummaryCalendarSlotStatus.Exam, exam.EquivalentStartSession, exam.EquivalentEndSession);
                }
            }

            foreach (var day in summary.days.Values)
            {
                day.CommitChanges();
            }

            summary.IsOnVacation = DateTimeOffset.Now > summary.TermRange.TermEndDate || DateTimeOffset.Now < summary.TermRange.TermStartDate;
            summary.IsTodayFree = !schedule.IsTodayOccupied && !exams.HasRecentExams && !summary.IsOnVacation;
        }

        private void AddScheduleToCalendar(DateTimeOffset time, SummaryCalendarSlotStatus status, int startSession, int endSession)
        {
            DateTimeOffset date = time.GetLocalDate();
            string key = date.ToString("s");
            SummaryCalendarDayViewModel day;
            if (days.ContainsKey(key))
            {
                day = days[key];
            }
            else
            {
                day = new SummaryCalendarDayViewModel(date);
                days.Add(day.Key, day);
                day.BeginChanges();
            }
            day.AddSchedule(status, startSession, endSession);
        }

        private void Calendar_CalendarViewDayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
        {
            DateTimeOffset date = args.Item.Date.GetLocalDate();
            string key = date.ToString("s");
            if (days.ContainsKey(key))
            {
                args.Item.DataContext = days[key];
            }
            else
            {
                SummaryCalendarDayViewModel dayVm = new SummaryCalendarDayViewModel(date);
                days.Add(dayVm.Key, dayVm);
                args.Item.DataContext = dayVm;
            }
        }

        private void Calendar_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            Analytics.TrackEvent("Schedule summary interaction");
            if (args.AddedDates.Count > 0)
            {
                DaySelectedMessage message = new DaySelectedMessage(args.AddedDates[0]);
                Application.Current.GetService<IMessageService<DaySelectedMessage>>().SendMessage(message);
            }
        }

        private Dictionary<string, SummaryCalendarDayViewModel> days = new Dictionary<string, SummaryCalendarDayViewModel>();
    }

    internal class ScheduleSummaryRecentExamsItemTemplateSelector : DataTemplateSelector
    {
        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is ExamViewModel exam)
            {
                if (exam.Countdown < 0)
                {
                    return SimpleTemplate;
                }
                else if (exam.Countdown < 2)
                {
                    return DetailedTemplate;
                }
                else
                {
                    return SimpleTemplate;
                }
            }
            else
            {
                throw new NotSupportedException($"This selector does not support items of type {item.GetType()}.");
            }
        }

        public DataTemplate SimpleTemplate { get; set; }
        public DataTemplate DetailedTemplate { get; set; }
    }
}
