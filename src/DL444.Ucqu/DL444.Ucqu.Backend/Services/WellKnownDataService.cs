using System;
using System.Collections.Generic;
using DL444.Ucqu.Backend.Models;
using Microsoft.Extensions.Configuration;

namespace DL444.Ucqu.Backend.Services
{
    public interface IWellknownDataService
    {
        string CurrentTerm { get; }
        DateTimeOffset TermStartDate { get; }
        IList<ScheduleTime> Schedule { get; }
    }

    internal class WellKnownDataService : IWellknownDataService
    {
        public WellKnownDataService(IConfiguration config)
        {
            CurrentTerm = config.GetValue<string>("Term:CurrentTerm");
            TermStartDate = DateTimeOffset.Parse(config.GetValue<string>("Term:TermStartDate"));
            List<ScheduleTime> scheduleItems = new List<ScheduleTime>();
            foreach (IConfigurationSection scheduleCfgItem in config.GetSection("Wellknown:Schedule").GetChildren())
            {
                string startTime = scheduleCfgItem.GetValue<string>("StartOffset");
                string endTime = scheduleCfgItem.GetValue<string>("EndOffset");
                scheduleItems.Add(new ScheduleTime(TimeSpan.Parse(startTime), TimeSpan.Parse(endTime)));
            }
            Schedule = scheduleItems.AsReadOnly();
        }

        public string CurrentTerm { get; }
        public DateTimeOffset TermStartDate { get; }
        public IList<ScheduleTime> Schedule { get; }
    }
}
