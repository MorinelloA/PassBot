namespace PassBot.Models
{
    public class UserPoints
    {
        public int Id { get; set; }
        public string DiscordId { get; set; }
        public string DiscordUsername { get; set; }
        public int Points { get; set; }
        public int TransferredPoints { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
