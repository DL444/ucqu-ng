using DL444.Ucqu.Client;

namespace DL444.Ucqu.Backend.Models
{
    public struct UserInitializeCommand
    {
        public UserInitializeCommand(SignInContext signInContext, string statusId)
        {
            SignInContext = signInContext;
            StatusId = statusId;
        }

        public SignInContext SignInContext { get; set; }
        public string StatusId { get; set; }
    }
}
