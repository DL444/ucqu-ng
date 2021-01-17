using System.Threading.Tasks;
using DL444.Ucqu.Models;

namespace DL444.Ucqu.Client
{
    public interface IUcquClient
    {
        Task<SignInResult> SignInAsync(string username, string passwordHash);
        Task<StudentInfo?> GetStudentInfoAsync();
        Task<ScoreSet?> GetScoreAsync(bool isSecondMajor);
        Task<Schedule?> GetScheduleAsync(int beginningYear, int term);
        Task<ExamSchedule?> GetExamScheduleAsync(int beginningYear, int term);
    }
}
