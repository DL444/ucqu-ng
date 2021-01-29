namespace DL444.Ucqu.Models
{
    public struct NotificationChannelItem
    {
        public NotificationChannelItem(string channelIdentifier) => ChannelIdentifier = channelIdentifier;
        public string ChannelIdentifier { get; set; }
    }
}
