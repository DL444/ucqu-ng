using System;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Extensions;
using Windows.Security.Credentials.UI;
using Windows.Storage;
using Windows.UI.Xaml;

namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal class WindowsHelloService : IWindowsHelloService
    {
        public bool IsEnabled
        {
            get
            {
                object obj = ApplicationData.Current.LocalSettings.Values["WinHelloEnabled"];
                if (obj == null || !(obj is bool enabled))
                {
                    return false;
                }
                return enabled;
            }
        }

        public async Task<bool> IsAvailableAsync() => await UserConsentVerifier.CheckAvailabilityAsync() == UserConsentVerifierAvailability.Available;

        public void Disable() => ApplicationData.Current.LocalSettings.Values["WinHelloEnabled"] = false;

        public async Task EnableAsync()
        {
            if (!await IsAvailableAsync())
            {
                return;
            }
            ApplicationData.Current.LocalSettings.Values["WinHelloEnabled"] = true;
        }

        public async Task<UserConsentVerificationResult> AuthenticateAsync()
        {
            ILocalizationService locService = Application.Current.GetService<ILocalizationService>();
            return await UserConsentVerifier.RequestVerificationAsync(locService.GetString("WindowsHelloAuthMessage"));
        }
    }
}
