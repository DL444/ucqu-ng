using System;
using System.Linq;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Exceptions;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Models;
using DL444.Ucqu.App.WinUniversal.Services;
using DL444.Ucqu.App.WinUniversal.ViewModels;
using DL444.Ucqu.Models;
using Windows.Foundation;
using Windows.Networking.PushNotifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
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
            IDataService localDataService = Application.Current.GetService<IDataService>(x => x.DataSource == DataSource.LocalCache);
            IDataService remoteDataService = Application.Current.GetService<IDataService>(x => x.DataSource == DataSource.Online);
            ILocalCacheService cacheService = Application.Current.GetService<ILocalCacheService>();
            NavigationView.SelectedItem = NavigationView.MenuItems.First();

            StudentInfoViewModel = new DataViewModel<StudentInfo, StudentInfoViewModel>(
                defaultValue: new StudentInfoViewModel(),
                viewModelTransform: x => new StudentInfoViewModel(x),
                localFetchFunc: () => localDataService.GetStudentInfoAsync(),
                remoteFetchFunc: () => remoteDataService.GetStudentInfoAsync(),
                cacheUpdateFunc: x => cacheService.SetStudentInfoAsync(x)
            );

            WellknownDataViewModel = new DataViewModel<WellknownData, WellknownDataViewModel>(
                defaultValue: new WellknownDataViewModel(new WellknownData()
                {
                    TermStartDate = DateTimeOffset.UtcNow.Date,
                    TermEndDate = DateTimeOffset.UtcNow.Date.AddDays(1)
                }),
                viewModelTransform: x => new WellknownDataViewModel(x),
                localFetchFunc: () => localDataService.GetWellknownDataAsync(),
                remoteFetchFunc: () => remoteDataService.GetWellknownDataAsync(),
                cacheUpdateFunc: x => cacheService.SetWellknownDataAsync(x),
                shouldFetchRemote: x => DateTimeOffset.UtcNow > x.TermEndDate,
                remoteRequiresAuth: false
            );

            ExamsViewModel = new DataViewModel<ExamSchedule, ExamScheduleViewModel>(
                defaultValue: new ExamScheduleViewModel(),
                viewModelTransform: x => new ExamScheduleViewModel(x, WellknownDataViewModel.Value.Model),
                localFetchFunc: () => localDataService.GetExamsAsync(),
                remoteFetchFunc: () => remoteDataService.GetExamsAsync(),
                cacheUpdateFunc: x => cacheService.SetExamsAsync(x),
                shouldFetchRemote: _ => DateTimeOffset.UtcNow < WellknownDataViewModel.Value.Model.TermEndDate
            );

            ScheduleViewModel = new DataViewModel<Schedule, ScheduleViewModel>(
                new ScheduleViewModel(),
                x => new ScheduleViewModel(x, WellknownDataViewModel.Value.Model),
                () => localDataService.GetScheduleAsync(),
                () => remoteDataService.GetScheduleAsync(),
                x => cacheService.SetScheduleAsync(x),
                _ => DateTimeOffset.UtcNow < WellknownDataViewModel.Value.Model.TermEndDate
            );
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            prevWidth = Window.Current.Bounds.Width;
            Window.Current.SizeChanged += CurrentWindow_SizeChanged;

            if (e.Parameter is string argument && argument.Equals("ScoreChanged", StringComparison.Ordinal))
            {
                NavigationView.SelectedItem = NavigationView.MenuItems.Last();
            }

            bool signedIn = Application.Current.GetService<ICredentialService>().IsSignedIn;

            IAsyncOperation<PushNotificationChannel> channelCreationTask = PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            Task wellknownUpdateTask = WellknownDataViewModel.StartUpdateAsync(signedIn);
            Task studentInfoUpdateTask = StudentInfoViewModel.StartUpdateAsync(signedIn);

            await wellknownUpdateTask;
            Task examsUpdateTask = ExamsViewModel.StartUpdateAsync(signedIn);
            Task scheduleUpdateTask = ScheduleViewModel.StartUpdateAsync(signedIn);

            if (!signedIn)
            {
                ISignInService signInService = Application.Current.GetService<ISignInService>();
                try
                {
                    await signInService.SignInAsync(false);
                    signedIn = true;
                    Application.Current.GetService<IMessageService<SignInMessage>>().SendMessage(new SignInMessage(true));
                }
                catch (BackendAuthenticationFailedException)
                {
                    await ((App)Application.Current).SignOutAsync();
                }
                catch (BackendRequestFailedException)
                {
                    // TODO: Log exception.
                    Application.Current.GetService<IMessageService<SignInMessage>>().SendMessage(new SignInMessage(false));
                }
            }

            if (signedIn)
            {
                INotificationChannelService channelService = Application.Current.GetService<INotificationChannelService>();
                Task channelUpdateTask;
                try
                {
                    PushNotificationChannel channel = await channelCreationTask;
                    channelUpdateTask = channelService.PostNotificationChannelAsync(new NotificationChannelItem(channel.Uri));
                }
                catch (Exception ex)
                {
                    // TODO: Log exception.
                    channelUpdateTask = Task.CompletedTask;
                }
                await Task.WhenAll(studentInfoUpdateTask, examsUpdateTask, scheduleUpdateTask, channelUpdateTask);
            }
            else
            {
                await Task.WhenAll(studentInfoUpdateTask, examsUpdateTask, scheduleUpdateTask);
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            Window.Current.SizeChanged -= CurrentWindow_SizeChanged;
            StudentInfoViewModel.Dispose();
            WellknownDataViewModel.Dispose();
            ExamsViewModel.Dispose();
            ScheduleViewModel.Dispose();
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
                case "About":
                    NavigationView.SelectedItem = null;
                    ContentFrame.Navigate(typeof(AboutPage));
                    break;
            }
        }

        private void GoToCalendarSubscriptionPage() => Navigate("CalendarSub");

        private void GoToSettingsPage() => Navigate("Settings");

        private void GoToAboutPage() => Navigate("About");

        private async Task SignOut() => await ((App)Application.Current).SignOutAsync();

        private bool topPaneOpen;
        private double prevWidth;
    }
}
