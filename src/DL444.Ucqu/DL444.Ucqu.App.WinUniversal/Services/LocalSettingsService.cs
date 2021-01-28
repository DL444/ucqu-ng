using System;
using DL444.Ucqu.App.WinUniversal.Extensions;
using Windows.Storage;
using Windows.UI.Xaml;

namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal class LocalSettingsService : ILocalSettingsService
    {
        public T GetValue<T>(string key) => GetValue<T>(key, default);

        public T GetValue<T>(string key, T defaultValue)
        {
            object obj = container.Values[key];
            if (obj == null)
            {
                container.Values[key] = defaultValue;
                return defaultValue;
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

        public void SetValue<T>(string key, T value) => container.Values[key] = value;

        public void Migrate()
        {
            if (!latestSettingsVersion.Equals(CurrentVersion, StringComparison.Ordinal))
            {
                MigrateTo20();
            }
            CurrentVersion = latestSettingsVersion;
        }

        private string CurrentVersion
        {
            get => GetValue<string>("SettingsVersion", null);
            set => SetValue("SettingsVersion", value);
        }

        private void MigrateTo20()
        {
            string studentId = GetValue<string>("id", null);
            string passwordHash = GetValue<string>("pwdHash", null);
            bool dailyToastSwitchOn = GetValue("dailyToastSwitch", true);
            container.Values.Clear();
            ICredentialService credentialService = Application.Current.GetService<ICredentialService>();
            if (!string.IsNullOrEmpty(studentId) && !string.IsNullOrEmpty(passwordHash))
            {
                credentialService.SetCredential(studentId, passwordHash);
            }
            else
            {
                // Uninstalling the app would not clear saved credentials, but users would expect so.
                // So clear it on initial migration.
                credentialService.ClearCredential();
            }
            SetValue("DailyToastEnabled", dailyToastSwitchOn);
        }

        private readonly ApplicationDataContainer container = ApplicationData.Current.LocalSettings;
        private readonly string latestSettingsVersion = "2.0";
    }
}
