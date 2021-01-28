using System.ComponentModel;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Exceptions;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Services;
using Windows.UI.Xaml;

namespace DL444.Ucqu.App.WinUniversal.ViewModels
{
    internal class SettingsViewModel : INotifyPropertyChanged
    {
        public SettingsViewModel()
        {
            settingsService = Application.Current.GetService<ILocalSettingsService>();
            winHelloService = Application.Current.GetService<IWindowsHelloService>();
        }

        public bool IsScheduleSummaryNotificationEnabled
        {
            get => settingsService.GetValue("DailyToastEnabled", true);
            set => settingsService.SetValue("DailyToastEnabled", value);
        }

        public bool IsWindowsHelloAvailable
        {
            get => _isWindowsHelloAvailable;
            set
            {
                _isWindowsHelloAvailable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsWindowsHelloAvailable)));
            }
        }

        public bool IsWindowsHelloEnabled => winHelloService.IsEnabled;

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
            IsWindowsHelloAvailable = await winHelloService.IsAvailableAsync();
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

        public async Task DeleteAccountAsync()
        {
            AccountDeleteInProgress = true;
            try
            {
                IDataService backendService = Application.Current.GetService<IDataService>(x => x.DataSource == DataSource.Online);
                await backendService.DeleteUserAsync();
                await ((App)Application.Current).SignOutAsync();
            }
            catch (BackendAuthenticationFailedException)
            {
                AccountDeleteReauthenticateRequired = true;
            }
            catch (BackendRequestFailedException)
            {
                AccountDeleteFailed = true;
            }
            finally
            {
                AccountDeleteInProgress = false;
            }
        }

        private readonly ILocalSettingsService settingsService;
        private readonly IWindowsHelloService winHelloService;
        private bool _isWindowsHelloAvailable;
        private bool _accountDeleteInProgress;
        private bool _accountDeleteFailed;
        private bool _accountDeleteReauthRequired;
    }
}
