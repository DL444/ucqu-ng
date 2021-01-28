using System;
using System.Threading.Tasks;

namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal interface INotificationService
    {
        Task UpdateScheduleSummaryNotificationAsync();
        void ClearToast(ToastTypes types);
    }

    [Flags]
    internal enum ToastTypes
    {
        None = 0,
        ScheduleSummary = 1,
        ScoreChange = 2,
        All = 3
    }
}
