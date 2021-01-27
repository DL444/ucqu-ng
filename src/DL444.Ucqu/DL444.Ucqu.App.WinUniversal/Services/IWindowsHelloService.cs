using System.Threading.Tasks;
using Windows.Security.Credentials.UI;

namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal interface IWindowsHelloService
    {
        bool IsEnabled { get; }
        Task<bool> IsAvailableAsync();
        Task EnableAsync();
        void Disable();
        Task<UserConsentVerificationResult> AuthenticateAsync();
    }
}
