using System.Collections.Generic;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Services;
using Microsoft.AppCenter.Analytics;
using Windows.Security.Credentials.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace DL444.Ucqu.App.WinUniversal.Pages
{
    public sealed partial class WindowsHelloAuthPage : Page
    {
        public WindowsHelloAuthPage()
        {
            this.InitializeComponent();
            winHelloService = Application.Current.GetService<IWindowsHelloService>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Analytics.TrackEvent("Windows Hello page reached");
            if (e.Parameter is string args)
            {
                arguments = args;
            }
            await Authenticate();
        }

        private async void Authenticate_Click(object sender, RoutedEventArgs e)
        {
            await Authenticate();
        }

        private async void SignOut_Click(object sender, RoutedEventArgs e)
        {
            winHelloService.Disable();
            await ((App)Application.Current).SignOutAsync();
        }

        private async Task Authenticate()
        {
            VisualStateManager.GoToState(this, "InProgress", false);
            UserConsentVerificationResult result = await winHelloService.AuthenticateAsync();
            Analytics.TrackEvent("Windows Hello authentication complete", new Dictionary<string, string>()
            {
                { "Result", result.ToString() }
            });
            switch (result)
            {
                case UserConsentVerificationResult.Verified:
                    ((App)Application.Current).NavigateToFirstPage(arguments, true);
                    return;
                case UserConsentVerificationResult.DeviceNotPresent:
                case UserConsentVerificationResult.NotConfiguredForUser:
                case UserConsentVerificationResult.DisabledByPolicy:
                case UserConsentVerificationResult.DeviceBusy:
                    WindowsHelloAuthError.Visibility = Visibility.Visible;
                    break;
            }
            VisualStateManager.GoToState(this, "Default", false);
        }

        private readonly IWindowsHelloService winHelloService;
        private string arguments;
    }
}
