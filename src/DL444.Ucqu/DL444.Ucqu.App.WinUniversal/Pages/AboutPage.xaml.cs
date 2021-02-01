using System;
using System.Collections.Generic;
using DL444.Ucqu.App.WinUniversal.Controls;
using Microsoft.AppCenter.Analytics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace DL444.Ucqu.App.WinUniversal.Pages
{
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Analytics.TrackEvent("Navigation", new Dictionary<string, string>()
            {
                { "Page", "About" }
            });
        }

        private async void LicenseNoticeButton_Click(object sender, RoutedEventArgs e)
        {
            OslNoticeDialog dialog = new OslNoticeDialog();
            await dialog.ShowAsync();
        }
    }
}
