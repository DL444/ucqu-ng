using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Models;
using DL444.Ucqu.App.WinUniversal.Services;
using DL444.Ucqu.App.WinUniversal.ViewModels;
using DL444.Ucqu.Models;
using Microsoft.AppCenter.Analytics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace DL444.Ucqu.App.WinUniversal.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SchedulePage : Page
    {
        public SchedulePage()
        {
            this.InitializeComponent();
            IDataService localDataService = Application.Current.GetService<IDataService>(x => x.DataSource == DataSource.LocalCache);
            IDataService remoteDataService = Application.Current.GetService<IDataService>(x => x.DataSource == DataSource.Online);

            WellknownDataViewModel = new DataViewModel<WellknownData, WellknownDataViewModel>(
                defaultValue: new WellknownDataViewModel(new WellknownData()
                {
                    TermStartDate = DateTimeOffset.UtcNow.Date,
                    TermEndDate = DateTimeOffset.UtcNow.Date.AddDays(1)
                }),
                viewModelTransform: x => new WellknownDataViewModel(x),
                localFetchFunc: () => localDataService.GetWellknownDataAsync(),
                remoteFetchFunc: () => remoteDataService.GetWellknownDataAsync(),
                cacheUpdateFunc: _ => Task.CompletedTask,
                shouldFetchRemote: x => DateTimeOffset.UtcNow > x.TermEndDate,
                remoteRequiresAuth: false
            );

            ScheduleViewModel = new DataViewModel<Schedule, ScheduleViewModel>(
                new ScheduleViewModel(),
                x => new ScheduleViewModel(x, WellknownDataViewModel.Value.Model),
                () => localDataService.GetScheduleAsync(),
                () => remoteDataService.GetScheduleAsync(),
                x => Task.CompletedTask,
                _ => DateTimeOffset.UtcNow < WellknownDataViewModel.Value.Model.TermEndDate
            );
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Analytics.TrackEvent("Navigation", new Dictionary<string, string>()
            {
                { "Page", "Schedule" }
            });
            Application.Current.GetService<IMessageService<DaySelectedMessage>>().Register(ScheduleTable);
            Application.Current.GetService<INotificationService>().ClearToast(ToastTypes.ScheduleSummary);
            bool signedIn = Application.Current.GetService<ICredentialService>().IsSignedIn;
            await WellknownDataViewModel.StartUpdateAsync(signedIn);
            await ScheduleViewModel.StartUpdateAsync(signedIn);
        }
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            Application.Current.GetService<IMessageService<DaySelectedMessage>>().Unregister(ScheduleTable);
        }

        internal DataViewModel<WellknownData, WellknownDataViewModel> WellknownDataViewModel { get; }
        internal DataViewModel<Schedule, ScheduleViewModel> ScheduleViewModel { get; }
    }
}
