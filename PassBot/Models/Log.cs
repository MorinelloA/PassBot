namespace PassBot.Models
{
    public class Log
    {
        public int Id { get; set; }
        public string DiscordId { get; set; }
        public string DiscordUsername { get; set; }
        public string Command { get; set; }
        public string Message { get; set; }
        public DateTime DateTime { get; set; }
    }
}

