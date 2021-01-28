using System;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Extensions;
using Windows.Security.Credentials.UI;
using Windows.UI.Xaml;

namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal class WindowsHelloService : IWindowsHelloService
    {
        public WindowsHelloService() => settingsService = Application.Current.GetService<ILocalSettingsService>();

        public bool IsEnabled => settingsService.GetValue("WinHelloEnabled", false);

        public async Task<bool> IsAvailableAsync() => await UserConsentVerifier.CheckAvailabilityAsync() == UserConsentVerifierAvailability.Available;

        public void Disable() => settingsService.SetValue("WinHelloEnabled", false);

        public async Task EnableAsync()
        {
            if (!await IsAvailableAsync())
            {
                return;
            }
            settingsService.SetValue("WinHelloEnabled", true);
        }

        public async Task<UserConsentVerificationResult> AuthenticateAsync()
        {
            ILocalizationService locService = Application.Current.GetService<ILocalizationService>();
            return await UserConsentVerifier.RequestVerificationAsync(locService.GetString("WindowsHelloAuthMessage"));
        }

        private ILocalSettingsService settingsService;
    }
}
