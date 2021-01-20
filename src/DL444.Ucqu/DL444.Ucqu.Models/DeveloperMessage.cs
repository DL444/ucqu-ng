using System;
using System.Collections.Generic;

namespace DL444.Ucqu.Models
{
    public class DeveloperMessage : ICosmosResource
    {
        public string Id() => "DevMessage";
        public string PartitionKey() => "DevMessage";

        public List<MessageItem> Messages { get; set; } = new List<MessageItem>();
    }

    public struct MessageItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public TargetPlatforms TargetPlatforms { get; set; }
        public DateTimeOffset Time { get; set; }
        public bool Archived { get; set; }
    }

    [Flags]
    public enum TargetPlatforms
    {
        None = 0,
        Android = 1,
        AppleDesktop = 2,
        AppleMobile = 4,
        Web = 8,
        Windows = 16,
        All = 255
    }
}
