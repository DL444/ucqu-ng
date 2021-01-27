using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Security.Credentials.UI;

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
            await ((App)Application.Current).SignOut();
        }

        private async Task Authenticate()
        {
            VisualStateManager.GoToState(this, "InProgress", false);
            UserConsentVerificationResult result = await winHelloService.AuthenticateAsync();
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

        private IWindowsHelloService winHelloService;
        private string arguments;
    }
}
