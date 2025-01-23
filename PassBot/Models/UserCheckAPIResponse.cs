namespace PassBot.Models
{
    public class UserCheckApiResponse
    {
        public UserCheckData Data { get; set; }
        public UserCheckMeta Meta { get; set; }
    }

    public class UserCheckData
    {
        public VerifiedEmail VerifiedEmail { get; set; }
        public VerifiedWalletAddress VerifiedWalletAddress { get; set; }
        public MatchStatus MatchStatus { get; set; }
    }

    public class VerifiedEmail
    {
        public string Email { get; set; }
        public bool IsPassEmail { get; set; }
    }

    public class VerifiedWalletAddress
    {
        public string WalletAddress { get; set; }
        public bool IsPassWalletAddress { get; set; }
    }

    public class MatchStatus
    {
        public bool IsEmailMatchWithWalletAddress { get; set; }
    }

    public class UserCheckMeta
    {
        // Add properties if needed; the "meta" object is empty in the provided JSON.
    }


}

