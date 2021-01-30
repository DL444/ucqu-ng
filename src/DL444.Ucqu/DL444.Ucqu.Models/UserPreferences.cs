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
    }
}
