using System;
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

        public void SignOut()
        {
            // TODO: Sign out.
        }

        private static void ConfigureConfiguration(IConfigurationBuilder builder) => builder.AddJsonFile("appconfig.json");

        private static void ConfigureServices(IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<ICredentialService>(new UserCredentialService());

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
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            ConfigureTitleBar();
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
                    rootFrame.Navigate(typeof(Pages.MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
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

        private void ConfigureTitleBar()
        {
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }
    }
}
