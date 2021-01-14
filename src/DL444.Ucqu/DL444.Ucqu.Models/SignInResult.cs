namespace DL444.Ucqu.Models
{
    public class SignInResult
    {
        public SignInResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
