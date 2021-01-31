﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using DL444.Ucqu.App.WinUniversal.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DL444.Ucqu.App.WinUniversal.Extensions;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;
using Windows.ApplicationModel.Background;
using System.Linq;
using Windows.UI.Notifications;
using DL444.Ucqu.App.WinUniversal.Exceptions;
using DL444.Ucqu.App.WinUniversal.Models;
using DL444.Ucqu.Models;

namespace DL444.Ucqu.App.WinUniversal
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            var configBuilder = new ConfigurationBuilder();
            ConfigureConfiguration(configBuilder);
            IConfiguration config = configBuilder.Build();
            Configuration = config;

            var services = new ServiceCollection();
            ConfigureServices(services, config);
            services.AddSingleton(config);
            Services = services.BuildServiceProvider();
        }

        public IConfiguration Configuration { get; }

        public IServiceProvider Services { get; }

        public void NavigateToFirstPage(string arguments = null, bool winHelloAuthenticated = false)
        {
            if (Window.Current.Content is Frame rootFrame)
            {
                ICredentialService credentialService = Services.GetService<ICredentialService>();
                if (credentialService.Username == null || credentialService.PasswordHash == null)
                {
                    rootFrame.Navigate(typeof(Pages.SignInPage), arguments, new DrillInNavigationTransitionInfo());
                }
                else
                {
                    IWindowsHelloService winHelloService = Services.GetService<IWindowsHelloService>();
                    if (!winHelloAuthenticated && winHelloService.IsEnabled)
                    {
                        rootFrame.Navigate(typeof(Pages.WindowsHelloAuthPage), arguments, new DrillInNavigationTransitionInfo());
                    }
                    else
                    {
                        rootFrame.Navigate(typeof(Pages.MainPage), arguments, new DrillInNavigationTransitionInfo());
                    }
                }
            }
        }

        public async Task SignOutAsync()
        {
            if (Window.Current.Content is Frame rootFrame)
            {
                ICredentialService credentialService = Services.GetService<ICredentialService>();
                ILocalCacheService cacheService = Services.GetService<ILocalCacheService>();
                credentialService.ClearCredential();
                await cacheService.ClearCacheAsync();
                rootFrame.Navigate(typeof(Pages.SignInPage), null, new DrillInNavigationTransitionInfo());
            }
        }

        private static void ConfigureConfiguration(IConfigurationBuilder builder) => builder.AddJsonFile("appconfig.json");

        private static void ConfigureServices(IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<ICredentialService>(new UserCredentialService());
            services.AddTransient<IWindowsHelloService, WindowsHelloService>();

            var baseAddress = new Uri(config.GetValue<string>("Backend:BaseAddress"));
            int retryCount = config.GetValue("Backend:RetryCount", 2);
            int timeout = config.GetValue("Backend:Timeout", 30);
            services.AddHttpClientWithDefaultPolicy<IDataService, BackendService>(baseAddress, retryCount, timeout);
            services.AddHttpClientWithDefaultPolicy<ISignInService, BackendService>(baseAddress, retryCount, timeout);
            services.AddHttpClientWithDefaultPolicy<ICalendarSubscriptionService, BackendService>(baseAddress, retryCount, timeout);
            services.AddHttpClientWithDefaultPolicy<INotificationChannelService, BackendService>(baseAddress, retryCount, timeout);
            services.AddHttpClientWithDefaultPolicy<IRemoteSettingsService, BackendService>(baseAddress, retryCount, timeout);

            LocalCacheService localCacheService = new LocalCacheService(config);
            services.AddSingleton<IDataService>(localCacheService);
            services.AddSingleton<ILocalCacheService>(localCacheService);

            services.AddSingleton<ILocalizationService, ResourceLocalizationService>();
            services.AddTransient<ILocalSettingsService, LocalSettingsService>();
            services.AddTransient<INotificationService, NotificationService>();

            services.AddMessageHub<SignInMessage, EventMessageService<SignInMessage>>();
            services.AddMessageHub<DaySelectedMessage, EventMessageService<DaySelectedMessage>>();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = await InitializeRootFrame(e);

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    NavigateToFirstPage(e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
            Frame rootFrame = await InitializeRootFrame(args);
            if (args is ToastNotificationActivatedEventArgs e)
            {
                NavigateToFirstPage(e.Argument);
            }
            else if (rootFrame.Content == null)
            {
                NavigateToFirstPage(null);
            }
            Window.Current.Activate();
        }

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);
            BackgroundTaskDeferral deferral = args.TaskInstance.GetDeferral();
            switch (args.TaskInstance.Task.Name)
            {
                case "BackgroundTaskReconfigureTask":
                    await ConfigureBackgroundTasksAsync(true);
                    break;
                case "NotificationUpdateTimerTask":
                    await Services.GetService<INotificationService>().UpdateScheduleSummaryNotificationAsync();
                    break;
                case "NotificationActivationTask":
                    var settingsService = Services.GetService<ILocalSettingsService>();
                    var triggerDetails = (ToastNotificationActionTriggerDetail)args.TaskInstance.TriggerDetails;
                    if ("NeverShowScheduleSummary".Equals(triggerDetails.Argument, StringComparison.Ordinal))
                    {
                        settingsService.SetValue("DailyToastEnabled", false);
                    }
                    else if ("NeverShowScoreChanged".Equals(triggerDetails.Argument, StringComparison.Ordinal))
                    {
                        string username = Services.GetService<ICredentialService>().Username;
                        if (username == null)
                        {
                            break;
                        }
                        var preferences = new Ucqu.Models.UserPreferences(username)
                        {
                            PreferenceItems = new Dictionary<string, string>()
                            {
                                { "ScoreChangeNotificationEnabled", "false" }
                            }
                        };
                        try
                        {
                            await Services.GetService<IRemoteSettingsService>().SetRemoteSettingsAsync(preferences);
                        }
                        catch (BackendRequestFailedException) { }
                    }
                    break;
                case "AppMaintenanceTask":
                    await PerformAppMaintenance();
                    break;
            }
            deferral.Complete();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        private void ConfigureWindow()
        {
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(500, 500));
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }

        private async Task ConfigureBackgroundTasksAsync(bool replaceIfExists)
        {
            BackgroundExecutionManager.RemoveAccess();
            BackgroundAccessStatus requestStatus = await BackgroundExecutionManager.RequestAccessAsync();
            if (requestStatus == BackgroundAccessStatus.AllowedSubjectToSystemPolicy || requestStatus == BackgroundAccessStatus.AlwaysAllowed)
            {
                var builder = new BackgroundTaskBuilder();
                builder.Name = "BackgroundTaskReconfigureTask";
                builder.SetTrigger(new SystemTrigger(SystemTriggerType.ServicingComplete, false));
                // Do NOT reregister migration task. A running migration task cannot unregister itself, so reregister will crash the app.
                // To modify this task in a future version, register a new task with a different name, and remove this one.
                TryRegisterBackgroundTask(builder, false);

                uint updateInterval = Configuration.GetValue<uint>("Notification:TimerTaskUpdateInterval", 15);
                updateInterval = Math.Max(15, updateInterval);
                builder = new BackgroundTaskBuilder();
                builder.Name = "NotificationUpdateTimerTask";
                builder.SetTrigger(new TimeTrigger(updateInterval, false));
                builder.AddCondition(new SystemCondition(SystemConditionType.BackgroundWorkCostNotHigh));
                TryRegisterBackgroundTask(builder, replaceIfExists);

                builder = new BackgroundTaskBuilder();
                builder.Name = "NotificationActivationTask";
                builder.SetTrigger(new ToastNotificationActionTrigger());
                TryRegisterBackgroundTask(builder, replaceIfExists);

                builder = new BackgroundTaskBuilder();
                builder.Name = "AppMaintenanceTask";
                builder.SetTrigger(new MaintenanceTrigger(1440, false));
                builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                builder.IsNetworkRequested = true;
                TryRegisterBackgroundTask(builder, replaceIfExists);
            }
        }

        private bool TryRegisterBackgroundTask(BackgroundTaskBuilder builder, bool replaceIfExists)
        {
            if (BackgroundTaskRegistration.AllTasks.Values.Any(x => x.Name.Equals(builder.Name, StringComparison.Ordinal)))
            {
                if (replaceIfExists)
                {
                    BackgroundTaskRegistration.AllTasks.Values.First(x => x.Name.Equals(builder.Name, StringComparison.Ordinal)).Unregister(true);
                    builder.Register();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                builder.Register();
                return true;
            }
        }

        private async Task<Frame> InitializeRootFrame(IActivatedEventArgs e)
        {
            ConfigureWindow();
            Services.GetService<ILocalSettingsService>().Migrate();
            await ConfigureBackgroundTasksAsync(false);
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            return rootFrame;
        }

        private async Task PerformAppMaintenance()
        {
            IDataService backendService = this.GetService<IDataService>(x => x.DataSource == DataSource.Online);
            ILocalCacheService cacheService = Services.GetService<ILocalCacheService>();
            Task<DataRequestResult<WellknownData>> wellknownTask = backendService.GetWellknownDataAsync();
            try
            {
                DataRequestResult<StudentInfo> studentInfo = await backendService.GetStudentInfoAsync();
                await cacheService.SetStudentInfoAsync(studentInfo.Resource);
            }
            catch (BackendRequestFailedException)
            {
                return;
            }
            catch (LocalCacheRequestFailedException) { }

            Task<DataRequestResult<Schedule>> scheduleTask = backendService.GetScheduleAsync();
            Task<DataRequestResult<ExamSchedule>> examsTask = backendService.GetExamsAsync();
            Task<DataRequestResult<ScoreSet>> majorScoreTask = backendService.GetScoreAsync(false);
            Task<DataRequestResult<ScoreSet>> secondMajorScoreTask = backendService.GetScoreAsync(true);

            try
            {
                DataRequestResult<WellknownData> wellknown = await wellknownTask;
                await cacheService.SetWellknownDataAsync(wellknown.Resource);
            }
            catch (Exception ex) when (ex is BackendRequestFailedException || ex is LocalCacheRequestFailedException) { }

            try
            {
                DataRequestResult<Schedule> schedule = await scheduleTask;
                await cacheService.SetScheduleAsync(schedule.Resource);
            }
            catch (Exception ex) when (ex is BackendRequestFailedException || ex is LocalCacheRequestFailedException) { }

            try
            {
                DataRequestResult<ExamSchedule> exams = await examsTask;
                await cacheService.SetExamsAsync(exams.Resource);
            }
            catch (Exception ex) when (ex is BackendRequestFailedException || ex is LocalCacheRequestFailedException) { }

            try
            {
                DataRequestResult<ScoreSet> majorScore = await majorScoreTask;
                await cacheService.SetScoreAsync(majorScore.Resource);
            }
            catch (Exception ex) when (ex is BackendRequestFailedException || ex is LocalCacheRequestFailedException) { }

            try
            {
                DataRequestResult<ScoreSet> secondMajorScore = await secondMajorScoreTask;
                await cacheService.SetScoreAsync(secondMajorScore.Resource);
            }
            catch (Exception ex) when (ex is BackendRequestFailedException || ex is LocalCacheRequestFailedException) { }
        }
    }
}
