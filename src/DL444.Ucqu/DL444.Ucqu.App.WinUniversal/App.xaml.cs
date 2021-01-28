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
            services.AddHttpClient<IDataService, BackendService>(client =>
            {
                client.BaseAddress = baseAddress;
            }).AddDefaultPolicy(retryCount, timeout);
            services.AddHttpClient<ISignInService, BackendService>(client =>
            {
                client.BaseAddress = baseAddress;
            }).AddDefaultPolicy(retryCount, timeout);
            services.AddHttpClient<ICalendarSubscriptionService, BackendService>(client =>
            {
                client.BaseAddress = baseAddress;
            }).AddDefaultPolicy(retryCount, timeout);

            LocalCacheService localCacheService = new LocalCacheService(config);
            services.AddSingleton<IDataService>(localCacheService);
            services.AddSingleton<ILocalCacheService>(localCacheService);

            services.AddTransient<ILocalizationService, ResourceLocalizationService>();
            services.AddTransient<ILocalSettingsService, LocalSettingsService>();
            services.AddTransient<INotificationService, NotificationService>();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
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
            }
            deferral.Complete();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
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
                builder.Name = $"BackgroundTaskReconfigureTask";
                builder.SetTrigger(new SystemTrigger(SystemTriggerType.ServicingComplete, false));
                // Do NOT reregister migration task. A running migration task cannot unregister itself, so reregister will crash the app.
                // To modify this task in a future version, register a new task with a different name, and remove this one.
                TryRegisterBackgroundTask(builder, false);

                uint updateInterval = Configuration.GetValue<uint>("Notification:TimerTaskUpdateInterval", 15);
                updateInterval = Math.Max(15, updateInterval);
                builder = new BackgroundTaskBuilder();
                builder.Name = $"NotificationUpdateTimerTask";
                builder.SetTrigger(new TimeTrigger(updateInterval, false));
                builder.AddCondition(new SystemCondition(SystemConditionType.BackgroundWorkCostNotHigh));
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
    }
}
