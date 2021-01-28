namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal interface ILocalSettingsService
    {
        T GetValue<T>(string key);
        T GetValue<T>(string key, T defaultValue);
        void SetValue<T>(string key, T value);
        void Migrate();
    }
}
