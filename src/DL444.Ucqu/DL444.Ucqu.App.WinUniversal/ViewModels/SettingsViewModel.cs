using System;
using System.ComponentModel;
using System.Threading.Tasks;
using DL444.Ucqu.App.WinUniversal.Exceptions;
using DL444.Ucqu.App.WinUniversal.Extensions;
using DL444.Ucqu.App.WinUniversal.Services;
using Windows.Storage;
using Windows.UI.Xaml;

namespace DL444.Ucqu.App.WinUniversal.ViewModels
{
    internal class SettingsViewModel : INotifyPropertyChanged
    {
        public SettingsViewModel()
        {
            container = ApplicationData.Current.LocalSettings;
            winHelloService = Application.Current.GetService<IWindowsHelloService>();
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
                await ((App)Application.Current).SignOut();
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

        private T GetValue<T>(string key)
        {
            object obj = container.Values[key];
            if (obj == null)
            {
                container.Values[key] = default(T);
                return default;
            }
            else if (obj is T value)
            {
                return value;
            }
            else
            {
                throw new InvalidCastException($"Value for key {key} is of type {obj.GetType()}, not {typeof(T)}.");
            }
        }

        private void SetValue<T>(string key, T value) => container.Values[key] = value;

        private ApplicationDataContainer container;
        private IWindowsHelloService winHelloService;
        private bool _isWindowsHelloAvailable;
        private bool _accountDeleteInProgress;
        private bool _accountDeleteFailed;
        private bool _accountDeleteReauthRequired;
    }
}
