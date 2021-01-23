using System.Threading.Tasks;
using DL444.Ucqu.Models;

namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal interface ILocalCacheService
    {
        Task SetWellknownDataAsync(WellknownData data);
        Task SetStudentInfoAsync(StudentInfo info);
        Task SetScheduleAsync(Schedule schedule);
        Task SetExamsAsync(ExamSchedule exams);
        Task SetScoreAsync(ScoreSet score);
        Task SetDeveloperMessagesAsync(DeveloperMessage messages);
        Task ClearCacheAsync();
    }
}
