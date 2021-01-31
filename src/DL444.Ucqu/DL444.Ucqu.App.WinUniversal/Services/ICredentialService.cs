namespace DL444.Ucqu.App.WinUniversal.Services
{
    internal interface ICredentialService
    {
        string Username { get; }
        string PasswordHash { get; }
        string Token { get; set; }
        bool IsSignedIn { get; }

        void SetCredential(string username, string passwordHash);
        void ClearCredential();
    }
}
