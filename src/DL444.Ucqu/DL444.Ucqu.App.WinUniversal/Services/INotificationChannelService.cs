using System.Threading.Tasks;
using DL444.Ucqu.Models;

namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal interface INotificationChannelService
    {
        Task PostNotificationChannelAsync(NotificationChannelItem channel);
    }
}
