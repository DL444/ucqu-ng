using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Models;
using DL444.Ucqu.Models;

namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal interface IDataService
    {
        DataSource DataSource { get; }

        Task<DataRequestResult<WellknownData>> GetWellknownDataAsync();
        Task<DataRequestResult<StudentInfo>> GetStudentInfoAsync();
        Task<DataRequestResult<Schedule>> GetScheduleAsync();
        Task<DataRequestResult<ExamSchedule>> GetExamsAsync();
        Task<DataRequestResult<ScoreSet>> GetScoreAsync(bool isSecondMajor);
        Task<DataRequestResult<object>> DeleteUserAsync();
        Task<DataRequestResult<DeveloperMessage>> GetDeveloperMessagesAsync();
    }

    internal enum DataSource
    {
        Online,
        LocalCache
    }
}
