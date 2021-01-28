namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal class ResourceLocalizationService : ILocalizationService
    {
        public string GetString(string key)
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse();
            return resourceLoader.GetString(key);
        }

        public string Format(string key, params object[] parameters)
        {
            return string.Format(GetString(key), parameters);
        }
    }
}
