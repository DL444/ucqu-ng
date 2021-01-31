using DL444.Ucqu.App.WinUniversal.Services;

namespace DL444.Ucqu.App.WinUniversal.Models
{
    internal struct SignInMessage : IMessage
    {
        public SignInMessage(bool success) => Success = success;
        public bool Success { get; set; }
    }
}
