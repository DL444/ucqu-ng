using DL444.Ucqu.App.WinUniversal.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace DL444.Ucqu.App.WinUniversal.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            ViewModel = new SettingsViewModel();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.UpdateAsync();
        }

        internal SettingsViewModel ViewModel { get; }

        private async void WindowsHello_Toggled(object sender, RoutedEventArgs e)
        {
            bool value = ((ToggleSwitch)sender).IsOn;
            await ViewModel.SetWindowsHelloEnabledAsync(value);
        }

        private async void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            DeleteAccountConfirmFlyout.Hide();
            await ViewModel.DeleteAccountAsync();
        }

        private async void ScoreChangedNotification_Toggled(object sender, RoutedEventArgs e)
        {
            bool value = ((ToggleSwitch)sender).IsOn;
            await ViewModel.SetScoreChangedNotificationEnabledAsync(value);
        }
    }
}
