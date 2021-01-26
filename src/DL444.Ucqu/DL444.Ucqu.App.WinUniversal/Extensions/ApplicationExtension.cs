using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Windows.UI.Xaml;

namespace DL444.Ucqu.App.WinUniversal.Extensions
{
    internal static class ApplicationExtension
    {
        public static T GetService<T>(this Application application)
        {
            return ((App)application).Services.GetService<T>();
        }

        public static T GetService<T>(this Application application, Predicate<T> condition)
        {
            return ((App)application).Services.GetServices<T>().FirstOrDefault(x => condition(x));
        }

        public static IConfiguration GetConfiguration(this Application application)
        {
            return ((App)application).Configuration;
        }

        public static T GetConfigurationValue<T>(this Application application, string key)
        {
            return GetConfigurationValue<T>(application, key, default(T));
        }

        public static T GetConfigurationValue<T>(this Application application, string key, T defaultVaule)
        {
            return GetConfiguration(application).GetValue<T>(key, defaultVaule);
        }
    }
}
