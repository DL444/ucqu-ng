using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Services;
using DL444.Ucqu.App.WinUniversal.ViewModels;
using DL444.Ucqu.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using WinUI = Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace DL444.Ucqu.App.WinUniversal.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            localDataService = Application.Current.GetService<IDataService>(x => x.DataSource == DataSource.LocalCache);
            remoteDataService = Application.Current.GetService<IDataService>(x => x.DataSource == DataSource.Online);
            cacheService = Application.Current.GetService<ILocalCacheService>();
            NavigationView.SelectedItem = NavigationView.MenuItems.First();
            StudentInfoViewModel = new DataViewModel<StudentInfo, StudentInfoViewModel>(new StudentInfoViewModel());
            WellknownDataViewModel = new DataViewModel<WellknownData, WellknownDataViewModel>(new WellknownDataViewModel(new WellknownData()
            {
                TermStartDate = DateTimeOffset.UtcNow.Date,
                TermEndDate = DateTimeOffset.UtcNow.Date.AddDays(1)
            }));
            ExamsViewModel = new DataViewModel<ExamSchedule, ExamScheduleViewModel>(new ExamScheduleViewModel());
            ScheduleViewModel = new DataViewModel<Schedule, ScheduleViewModel>(new ScheduleViewModel());
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            prevWidth = Window.Current.Bounds.Width;
            Window.Current.SizeChanged += CurrentWindow_SizeChanged;
            var wellknownUpdateTask = WellknownDataViewModel.UpdateAsync(
                () => localDataService.GetWellknownDataAsync(),
                () => remoteDataService.GetWellknownDataAsync(),
                x => cacheService.SetWellknownDataAsync(x),
                x => new WellknownDataViewModel(x),
                preferLocal: true,
                x => DateTimeOffset.UtcNow > x.TermEndDate);
            var studentInfoUpdateTask = StudentInfoViewModel.UpdateAsync(
                () => localDataService.GetStudentInfoAsync(),
                () => remoteDataService.GetStudentInfoAsync(),
                x => cacheService.SetStudentInfoAsync(x),
                x => new StudentInfoViewModel(x)
            );
            await wellknownUpdateTask;
            Task examsUpdateTask = ExamsViewModel.UpdateAsync(
                () => localDataService.GetExamsAsync(),
                () => remoteDataService.GetExamsAsync(),
                x => cacheService.SetExamsAsync(x),
                x => new ExamScheduleViewModel(x, WellknownDataViewModel.Value.Model),
                true,
                _ => DateTimeOffset.UtcNow < WellknownDataViewModel.Value.Model.TermEndDate);
            Task scheduleUpdateTask = ScheduleViewModel.UpdateAsync(
                () => localDataService.GetScheduleAsync(),
                () => remoteDataService.GetScheduleAsync(),
                x => cacheService.SetScheduleAsync(x),
                x => new ScheduleViewModel(x, WellknownDataViewModel.Value.Model),
                true,
                _ => DateTimeOffset.UtcNow < WellknownDataViewModel.Value.Model.TermEndDate);
            await Task.WhenAll(studentInfoUpdateTask, examsUpdateTask, scheduleUpdateTask);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            Window.Current.SizeChanged -= CurrentWindow_SizeChanged;
        }

        internal DataViewModel<StudentInfo, StudentInfoViewModel> StudentInfoViewModel { get; }
        internal DataViewModel<WellknownData, WellknownDataViewModel> WellknownDataViewModel { get; }
        internal DataViewModel<ExamSchedule, ExamScheduleViewModel> ExamsViewModel { get; }
        internal DataViewModel<Schedule, ScheduleViewModel> ScheduleViewModel { get; }

        private void PaneToggleButton_Click(object sender, RoutedEventArgs e)
        {
            SetTopPaneOpenAsync(!topPaneOpen, true);
        }

        private void TopPaneLightDismissTarget_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SetTopPaneOpenAsync(false, true);
        }

        private void CurrentWindow_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            if (prevWidth >= 1008 && e.Size.Width < 1008)
            {
                SetTopPaneOpenAsync(false, false);
            }
            prevWidth = e.Size.Width;
        }

        private void NavigationView_SelectionChanged(WinUI.NavigationView sender, WinUI.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is WinUI.NavigationViewItem item && item.Tag is string key)
            {
                Navigate(key);
            }
        }

        private void SetTopPaneOpenAsync(bool value, bool showAnimation)
        {
            if (topPaneOpen == value)
            {
                return;
            }
            topPaneOpen = value;
            PaneToggleIcon.CenterPoint = new System.Numerics.Vector3((float)PaneToggleIcon.ActualWidth / 2.0f, (float)PaneToggleIcon.ActualHeight / 2.0f, 0.0f);
            if (value == true)
            {
                PaneToggleIcon.Rotation = 180;
                if (showAnimation)
                {
                    TopPaneShowAnimation.Begin();
                }
                else
                {
                    TopPaneLightDismissTarget.Visibility = Visibility.Visible;
                    SummaryPane.Visibility = Visibility.Visible;
                }
            }
            else
            {
                PaneToggleIcon.Rotation = 0;
                if (showAnimation)
                {
                    TopPaneHideAnimation.Begin();
                }
                else
                {
                    TopPaneLightDismissTarget.Visibility = Visibility.Collapsed;
                    SummaryPane.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Navigate(string key)
        {
            switch (key)
            {
                case "Schedule":
                    if (ContentFrame.CurrentSourcePageType == typeof(ScorePage))
                    {
                        ContentFrame.Navigate(typeof(SchedulePage), null, new SlideNavigationTransitionInfo()
                        {
                            Effect = SlideNavigationTransitionEffect.FromLeft
                        });
                    }
                    else
                    {
                        ContentFrame.Navigate(typeof(SchedulePage), null, new EntranceNavigationTransitionInfo());
                    }
                    break;
                case "Score":
                    if (ContentFrame.CurrentSourcePageType == typeof(SchedulePage))
                    {
                        ContentFrame.Navigate(typeof(ScorePage), null, new SlideNavigationTransitionInfo()
                        {
                            Effect = SlideNavigationTransitionEffect.FromRight
                        });
                    }
                    else
                    {
                        ContentFrame.Navigate(typeof(ScorePage), null, new EntranceNavigationTransitionInfo());
                    }
                    break;
                case "CalendarSub":
                    if (!StudentInfoViewModel.IsValueReady)
                    {
                        return;
                    }
                    NavigationView.SelectedItem = null;
                    ContentFrame.Navigate(typeof(CalendarSubscriptionPage), StudentInfoViewModel.Value.StudentId);
                    break;
                case "Settings":
                    NavigationView.SelectedItem = null;
                    ContentFrame.Navigate(typeof(SettingsPage));
                    break;
            }
        }

        private void GoToCalendarSubscriptionPage() => Navigate("CalendarSub");

        private void GoToSettingsPage() => Navigate("Settings");

        private async Task SignOut() => await ((App)Application.Current).SignOutAsync();

        private IDataService localDataService;
        private IDataService remoteDataService;
        private ILocalCacheService cacheService;
        private bool topPaneOpen;
        private double prevWidth;
    }
}
