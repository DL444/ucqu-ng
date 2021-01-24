namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal interface ILocalizationService
    {
        string GetString(string key);
        string Format(string key, params object[] parameters);
    }
}
