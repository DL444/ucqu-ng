using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Models;
using DL444.Ucqu.Models;

namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal interface ICalendarSubscriptionService
    {
        Task<DataRequestResult<CalendarSubscription>> GetCalendarSubscriptionIdAsync();
        Task<DataRequestResult<string>> GetCalendarSubscriptionContentAsync(string username, string subscriptionId);
        Task<DataRequestResult<CalendarSubscription>> ResetCalendarSubscriptionAsync();
    }
}
