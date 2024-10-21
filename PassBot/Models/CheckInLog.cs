namespace PassBot.Models
{
    public class CheckInLog
    {
        public long Id { get; set; }
        public string DiscordId { get; set; }
        public DateTime LastCheckIn { get; set; }
        public string DiscordUsername { get; set; }
        public int CheckInIterator { get; set; }
        public TimeSpan? RemainingTime { get; set; }
        public bool IsAllowed { get; set; }


    }

}

