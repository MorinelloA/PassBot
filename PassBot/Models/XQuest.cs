namespace PassBot.Models
{
    public class XQuest
    {
        public long Id { get; set; }
        public string Link { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public long ExpiresAt { get; set; }
        public DateTime? DistributedOn { get; set; }
    }
}
