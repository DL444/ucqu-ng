using System.Collections.Generic;

namespace DL444.Ucqu.Models
{
    public class UserPreferences : ICosmosResource
    {
        public UserPreferences(string username) => Username = username;

        public string Id() => $"Preferences-{Username}";
        public string PartitionKey() => Username;

        public string Username { get; set; }
        public Dictionary<string, string> PreferenceItems { get; set; } = new Dictionary<string, string>();

        public string GetValue(string key, string defaultValue)
        {
            if (PreferenceItems.TryGetValue(key, out string value))
            {
                return value;
            }
            else
            {
                return defaultValue;
            }
        }
    }
}
