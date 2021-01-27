using System;
using System.ComponentModel;
using System.Threading.Tasks;
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

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task Update()
        {
            IsWindowsHelloAvailable = await winHelloService.IsAvailableAsync();
        }

        public async Task SetWindowsHelloEnabled(bool value)
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
    }
}
