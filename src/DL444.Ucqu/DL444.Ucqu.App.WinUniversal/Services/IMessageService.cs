namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal interface IMessageService<T> where T : IMessage
    {
        void Register(IMessageListener<T> listener);
        void Unregister(IMessageListener<T> listener);
        void SendMessage(T message);
    }

    internal interface IMessageListener<T> where T : IMessage
    {
        void OnMessaged(T args);
    }

    internal interface IMessage { }
}
