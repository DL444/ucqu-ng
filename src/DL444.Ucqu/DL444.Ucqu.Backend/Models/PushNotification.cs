namespace DL444.Ucqu.Backend.Models
{
    public struct WindowsPushNotification
    {
        public WindowsPushNotification(WindowsNotificationType type, string payload)
        {
            Type = type;
            Payload = payload;
        }

        public WindowsNotificationType Type { get; set; }
        public string Payload { get; set; }
    }

    public enum WindowsNotificationType
    {
        Badge,
        Raw,
        Tile,
        Toast
    }
}
