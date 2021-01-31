namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal class EventMessageService<T> : IMessageService<T> where T : IMessage
    {
        public void Register(IMessageListener<T> listener)
        {
            MessageEvent += listener.OnMessaged;
        }

        public void Unregister(IMessageListener<T> listener)
        {
            MessageEvent -= listener.OnMessaged;
        }

        public void SendMessage(T message)
        {
            MessageEvent?.Invoke(message);
        }

        private event EventMessageHandler MessageEvent;

        private delegate void EventMessageHandler(T message);
    }
}
