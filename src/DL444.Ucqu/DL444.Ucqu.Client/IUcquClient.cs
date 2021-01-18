using System.Threading.Tasks;
using DL444.Ucqu.Models;

namespace DL444.Ucqu.Client
{
    public interface IUcquClient
    {
        Task<SignInContext> SignInAsync(string username, string passwordHash);
        Task<StudentInfo?> GetStudentInfoAsync(SignInContext signInContext);
        Task<ScoreSet?> GetScoreAsync(SignInContext signInContext, bool isSecondMajor);
        Task<Schedule?> GetScheduleAsync(SignInContext signInContext, int beginningYear, int term);
        Task<ExamSchedule?> GetExamScheduleAsync(SignInContext signInContext, int beginningYear, int term);
    }
}
