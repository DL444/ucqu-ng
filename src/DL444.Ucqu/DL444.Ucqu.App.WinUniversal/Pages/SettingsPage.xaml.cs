using System.Collections.Generic;
using DL444.Ucqu.App.WinUniversal.ViewModels;
using Microsoft.AppCenter.Analytics;
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
            Analytics.TrackEvent("Navigation", new Dictionary<string, string>()
            {
                { "Page", "Settings" }
            });
            await ViewModel.UpdateAsync();
        }

        internal SettingsViewModel ViewModel { get; }

        private async void WindowsHello_Toggled(object sender, RoutedEventArgs e)
        {
            bool value = ((ToggleSwitch)sender).IsOn;
            Analytics.TrackEvent("Settings toggled", new Dictionary<string, string>()
            {
                { "Settings", $"WindowsHello" },
                { "Value", $"{value}" }
            });
            await ViewModel.SetWindowsHelloEnabledAsync(value);
        }

        private void DeleteAccountPreview_Click(object sender, RoutedEventArgs e)
        {
            Analytics.TrackEvent("Account deletion requested");
        }

        private async void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            Analytics.TrackEvent("Account deletion confirmed");
            DeleteAccountConfirmFlyout.Hide();
            await ViewModel.DeleteAccountAsync();
        }

        private async void ScoreChangedNotification_Toggled(object sender, RoutedEventArgs e)
        {
            bool value = ((ToggleSwitch)sender).IsOn;
            Analytics.TrackEvent("Settings toggled", new Dictionary<string, string>()
            {
                { "Settings", $"ScoreChangeNotification" },
                { "Value", $"{value}" }
            });
            await ViewModel.SetScoreChangedNotificationEnabledAsync(value);
        }
    }
}
