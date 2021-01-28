using System;
using DL444.Ucqu.App.WinUniversal.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DL444.Ucqu.App.WinUniversal.Pages
{
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();
        }

        private async void LicenseNoticeButton_Click(object sender, RoutedEventArgs e)
        {
            OslNoticeDialog dialog = new OslNoticeDialog();
            await dialog.ShowAsync();
        }
    }
}
