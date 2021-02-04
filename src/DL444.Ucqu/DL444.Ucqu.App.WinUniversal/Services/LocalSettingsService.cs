using System;
using System.Linq;
using DL444.Ucqu.App.WinUniversal.Extensions;
using Windows.ApplicationModel.Background;
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
                MigrateTo201();
            }
            CurrentVersion = latestSettingsVersion;
        }

        private string CurrentVersion
        {
            get => GetValue<string>("SettingsVersion", null);
            set => SetValue("SettingsVersion", value);
        }

        private void MigrateTo201()
        {
            if ("2.0.1".Equals(CurrentVersion, StringComparison.Ordinal))
            {
                return;
            }
            MigrateTo20();
            IBackgroundTaskRegistration legacyTimerUpdateTask = BackgroundTaskRegistration.AllTasks.Values.FirstOrDefault(x => "Hourly Tile Update Task".Equals(x.Name, StringComparison.Ordinal));
            if (legacyTimerUpdateTask != null)
            {
                legacyTimerUpdateTask.Unregister(false);
            }
            IBackgroundTaskRegistration legacyLoginUpdateTask = BackgroundTaskRegistration.AllTasks.Values.FirstOrDefault(x => "Login Tile Update Task".Equals(x.Name, StringComparison.Ordinal));
            if (legacyLoginUpdateTask != null)
            {
                legacyLoginUpdateTask.Unregister(false);
            }
            CurrentVersion = "2.0.1";
        }

        private void MigrateTo20()
        {
            if ("2.0".Equals(CurrentVersion, StringComparison.Ordinal))
            {
                return;
            }
            string studentId = GetValue<string>("id", null);
            string passwordHash = GetValue<string>("pwdHash", null);
            string dailyToastSwitchOn = GetValue("dailyToastSwitch", "on");
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
            if ("off".Equals(dailyToastSwitchOn, StringComparison.Ordinal))
            {
                SetValue("DailyToastEnabled", false);
            }
            else
            {
                SetValue("DailyToastEnabled", true);
            }
            CurrentVersion = "2.0";
        }

        private readonly ApplicationDataContainer container = ApplicationData.Current.LocalSettings;
        private readonly string latestSettingsVersion = "2.0.1";
    }
}
