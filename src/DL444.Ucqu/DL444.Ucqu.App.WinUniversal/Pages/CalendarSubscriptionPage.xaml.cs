using System;
using System.Collections.Generic;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Services;
using DL444.Ucqu.App.WinUniversal.ViewModels;
using Microsoft.AppCenter.Analytics;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace DL444.Ucqu.App.WinUniversal.Pages
{
    public sealed partial class CalendarSubscriptionPage : Page
    {
        public CalendarSubscriptionPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Analytics.TrackEvent("Navigation", new Dictionary<string, string>()
            {
                { "Page", "CalendarSubscription" }
            });
            if (!(e.Parameter is string username))
            {
                throw new ArgumentException("Required username parameter is not provided or not recognized.");
            }
            ICalendarSubscriptionService service = Application.Current.GetService<ICalendarSubscriptionService>();
            ViewModel = new CalendarSubscriptionViewModel(username, service);
            await ViewModel.UpdateAsync();
        }

        internal CalendarSubscriptionViewModel ViewModel { get; private set; }

        private void CopyUri_Click(object sender, RoutedEventArgs e)
        {
            DataPackage data = new DataPackage();
            data.RequestedOperation = DataPackageOperation.Copy;
            data.SetText(ViewModel.GenericHttpsUri);
            Clipboard.SetContent(data);
            Analytics.TrackEvent("Calendar subscription copied");
        }

        private async void SaveCalendarFile_Click(object sender, RoutedEventArgs e)
        {
            Analytics.TrackEvent("Calendar file downloading");
            string content = await ViewModel.GetSubscriptionContentAsync();
            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            ILocalizationService locService = Application.Current.GetService<ILocalizationService>();
            string fileTypeName = locService.GetString("IcsFileTypeName");
            string filename = locService.GetString("DefaultScheduleIcsName");
            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeChoices.Add(fileTypeName, new[] { ".ics" });
            picker.SuggestedFileName = filename;
            StorageFile file = await picker.PickSaveFileAsync();
            if (file == null)
            {
                Analytics.TrackEvent("Calendar file save canceled");
                return;
            }
            CachedFileManager.DeferUpdates(file);
            await FileIO.WriteTextAsync(file, content);
            await CachedFileManager.CompleteUpdatesAsync(file);
        }
    }
}
