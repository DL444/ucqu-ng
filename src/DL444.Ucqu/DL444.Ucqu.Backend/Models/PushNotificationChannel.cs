using System.Collections.Generic;
using DL444.Ucqu.Backend.Services;
using DL444.Ucqu.Models;

namespace DL444.Ucqu.Backend.Models
{
    public class NotificationChannelCollection : ICosmosResource
    {
        public NotificationChannelCollection(string username, PushPlatform platform)
        {
            Username = username;
            Platform = platform;
        }

        public string Id() => $"PushChannels-{Platform}-{Username}";
        public string PartitionKey() => Username;

        public string Username { get; set; }
        public PushPlatform Platform { get; set; }
        public List<NotificationChannelItem> Channels { get; set; } = new List<NotificationChannelItem>();
    }
}
