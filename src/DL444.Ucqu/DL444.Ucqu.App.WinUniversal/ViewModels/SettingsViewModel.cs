using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Exceptions;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Models;
using DL444.Ucqu.App.WinUniversal.Services;
using DL444.Ucqu.Models;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Windows.UI.Xaml;

namespace DL444.Ucqu.App.WinUniversal.ViewModels
{
    internal class SettingsViewModel : INotifyPropertyChanged
    {
        public SettingsViewModel()
        {
            remoteSettingsService = Application.Current.GetService<IRemoteSettingsService>();
            settingsService = Application.Current.GetService<ILocalSettingsService>();
            winHelloService = Application.Current.GetService<IWindowsHelloService>();
            username = Application.Current.GetService<ICredentialService>().Username;
        }

        public bool IsScheduleSummaryNotificationEnabled
        {
            get => settingsService.GetValue("DailyToastEnabled", true);
            set
            {
                Analytics.TrackEvent("Settings toggled", new Dictionary<string, string>()
                {
                    { "Settings", $"DailyToast" },
                    { "Value", $"{value}" }
                });
                settingsService.SetValue("DailyToastEnabled", value);
            }
        }

        public bool IsScoreChangeNotificationEnabled
        {
            get => _isScoreChangeNotificationEnabled;
            private set
            {
                _isScoreChangeNotificationEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsScoreChangeNotificationEnabled)));
            }
        }

        public bool IsScoreChangedNotificationEnabledUpdateInProgress
        {
            get => _isScoreChangedNotificationEnabledUpdateInProgress;
            private set
            {
                _isScoreChangedNotificationEnabledUpdateInProgress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsScoreChangedNotificationEnabledUpdateInProgress)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsScoreChangedNotificationEnabledUpdateCompleted)));
            }
        }

        public bool IsScoreChangedNotificationEnabledUpdateFailed
        {
            get => _isScoreChangedNotificationEnabledUpdateFailed;
            private set
            {
                _isScoreChangedNotificationEnabledUpdateFailed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsScoreChangedNotificationEnabledUpdateFailed)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsScoreChangedNotificationEnabledUpdateCompleted)));
            }
        }

        public bool IsScoreChangedNotificationEnabledUpdateCompleted
            => !IsScoreChangedNotificationEnabledUpdateInProgress && !IsScoreChangedNotificationEnabledUpdateFailed;

        public bool IsWindowsHelloAvailable
        {
            get => _isWindowsHelloAvailable;
            private set
            {
                _isWindowsHelloAvailable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsWindowsHelloAvailable)));
            }
        }

        public bool IsWindowsHelloEnabled => winHelloService.IsEnabled;

        public bool IsTelemetryEnabled
        {
            get => AppCenter.IsEnabledAsync().Result;
            set
            {
                if (value == false)
                {
                    Analytics.TrackEvent("Settings toggled", new Dictionary<string, string>()
                    {
                        { "Settings", $"Telemetry" },
                        { "Value", $"{value}" }
                    });
                }
                AppCenter.SetEnabledAsync(value).Wait();
                if (value == true)
                {
                    Analytics.TrackEvent("Settings toggled", new Dictionary<string, string>()
                    {
                        { "Settings", $"Telemetry" },
                        { "Value", $"{value}" }
                    });
                }
            }
        }

        public bool AccountDeleteInProgress
        {
            get => _accountDeleteInProgress;
            private set
            {
                _accountDeleteInProgress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccountDeleteInProgress)));
            }
        }

        public bool AccountDeleteFailed
        {
            get => _accountDeleteFailed;
            private set
            {
                _accountDeleteFailed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccountDeleteFailed)));
            }
        }

        public bool AccountDeleteReauthenticateRequired
        {
            get => _accountDeleteReauthRequired;
            private set
            {
                _accountDeleteReauthRequired = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccountDeleteReauthenticateRequired)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task UpdateAsync()
        {
            IsScoreChangedNotificationEnabledUpdateInProgress = true;
            IsWindowsHelloAvailable = await winHelloService.IsAvailableAsync();
            try
            {
                DataRequestResult<UserPreferences> preferences = await remoteSettingsService.GetRemoteSettingsAsync();
                UpdateRemoteSettings(preferences.Resource);
                bool success = bool.TryParse(preferences.Resource.GetValue("ScoreChangeNotificationEnabled", "true"), out bool scoreChangeNotificationEnabled);
                if (!success)
                {
                    scoreChangeNotificationEnabled = true;
                }
                IsScoreChangeNotificationEnabled = scoreChangeNotificationEnabled;
            }
            catch (BackendRequestFailedException ex)
            {
                Crashes.TrackError(ex);
                IsScoreChangedNotificationEnabledUpdateFailed = true;
            }
            finally
            {
                IsScoreChangedNotificationEnabledUpdateInProgress = false;
            }
        }

        public async Task SetWindowsHelloEnabledAsync(bool value)
        {
            if (value == IsWindowsHelloEnabled)
            {
                return;
            }
            if (value == false)
            {
                winHelloService.Disable();
            }
            else if (IsWindowsHelloAvailable)
            {
                await winHelloService.EnableAsync();
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsWindowsHelloEnabled)));
        }

        public async Task SetScoreChangedNotificationEnabledAsync(bool value)
        {
            if (value == IsScoreChangeNotificationEnabled)
            {
                return;
            }
            var preferences = new UserPreferences(username)
            {
                PreferenceItems = new Dictionary<string, string>()
                {
                    { "ScoreChangeNotificationEnabled", value.ToString() }
                }
            };
            IsScoreChangedNotificationEnabledUpdateInProgress = true;
            try
            {
                DataRequestResult<UserPreferences> result = await remoteSettingsService.SetRemoteSettingsAsync(preferences);
                UpdateRemoteSettings(result.Resource);
            }
            catch (BackendRequestFailedException ex)
            {
                Crashes.TrackError(ex);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsScoreChangeNotificationEnabled)));
            }
            finally
            {
                IsScoreChangedNotificationEnabledUpdateInProgress = false;
            }
        }

        public async Task DeleteAccountAsync()
        {
            AccountDeleteInProgress = true;
            try
            {
                IDataService backendService = Application.Current.GetService<IDataService>(x => x.DataSource == DataSource.Online);
                await backendService.DeleteUserAsync();
                Analytics.TrackEvent("Account deleted");
                await ((App)Application.Current).SignOutAsync();
            }
            catch (BackendAuthenticationFailedException ex)
            {
                Crashes.TrackError(ex);
                Analytics.TrackEvent("Authentication failed");
                AccountDeleteReauthenticateRequired = true;
            }
            catch (BackendRequestFailedException ex)
            {
                Crashes.TrackError(ex);
                AccountDeleteFailed = true;
            }
            finally
            {
                AccountDeleteInProgress = false;
            }
        }

        private void UpdateRemoteSettings(UserPreferences preferences)
        {
            bool success = bool.TryParse(preferences.GetValue("ScoreChangeNotificationEnabled", "true"), out bool scoreChangeNotificationEnabled);
            if (!success)
            {
                scoreChangeNotificationEnabled = true;
            }
            IsScoreChangeNotificationEnabled = scoreChangeNotificationEnabled;
        }

        private readonly IRemoteSettingsService remoteSettingsService;
        private readonly ILocalSettingsService settingsService;
        private readonly IWindowsHelloService winHelloService;
        private readonly string username;
        private bool _isScoreChangedNotificationEnabledUpdateInProgress;
        private bool _isScoreChangedNotificationEnabledUpdateFailed;
        private bool _isScoreChangeNotificationEnabled;
        private bool _isWindowsHelloAvailable;
        private bool _accountDeleteInProgress;
        private bool _accountDeleteFailed;
        private bool _accountDeleteReauthRequired;
    }
}
