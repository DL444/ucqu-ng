namespace DL444.Ucqu.Client
{
    public struct SignInContext
    {
        public SignInContext(SignInResult signInResult, string? sessionId, string? signedInUser)
        {
            Result = signInResult;
            SessionId = sessionId;
            SignedInUser = signedInUser;
        }

        public SignInResult Result { get; set; }
        public string? SessionId { get; set; }
        public string? SignedInUser { get; set; }
        public bool IsValid => Result == SignInResult.Success && SessionId != null && SignedInUser != null;
    }

    public enum SignInResult
    {
        Success = 0,
        InvalidCredentials = 1,
        NotRegistered = 2,
        InvalidCredentialsUserInexist = 3
    }
}
