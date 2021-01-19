namespace DL444.Ucqu.Models
{
    public struct CalendarSubscription
    {
        public CalendarSubscription(string subscriptionId) => SubscriptionId = subscriptionId;

        public string SubscriptionId { get; set; }
    }
}
