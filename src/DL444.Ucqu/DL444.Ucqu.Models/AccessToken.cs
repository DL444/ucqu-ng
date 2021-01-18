namespace DL444.Ucqu.Models
{
    public struct AccessToken
    {
        public bool Completed { get; set; }
        public string? Location { get; set; }
        public string? Token { get; set; }

        public static AccessToken CompletedToken(string token) => new AccessToken()
        {
            Completed = true,
            Token = token
        };

        public static AccessToken IncompleteToken(string token, string location) => new AccessToken()
        {
            Completed = false,
            Location = location,
            Token = token
        };
    }
}
