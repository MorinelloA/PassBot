namespace PassBot.Models
{
    public class ProfileChangeLog
    {
        public int Id { get; set; }
        public string DiscordId { get; set; }
        public string DiscordUsername { get; set; }
        public string ChangedItem { get; set; }
        public DateTime ChangedTime { get; set; }
        public string ChangedTo { get; set; }
    }
}

