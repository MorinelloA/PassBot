﻿namespace PassBot.Models
{
    public class UserProfile
    {
        public int Id { get; set; }
        public string DiscordId { get; set; }
        public string DiscordUsername { get; set; }
        public string Email { get; set; }
        public string WalletAddress { get; set; }
        public string XAccount { get; set; }
    }
}
