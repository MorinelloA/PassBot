namespace PassBot.Models
{
    public class Redemption
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string RedemptionId { get; set; } // NanoID
        public string DiscordId { get; set; }
        public string DiscordUsername { get; set; }
        public DateTime ClaimedOn { get; set; }
        public string SentBy { get; set; }
        public DateTime? SentOn { get; set; }
        public long Spent { get; set; }
        public string ItemName { get; set; } // Not in Redemption Table. Its in Item table instead
    }

}

