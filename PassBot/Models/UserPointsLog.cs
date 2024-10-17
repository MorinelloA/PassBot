namespace PassBot.Models
{
    public class UserPointsLog
    {
        public int Id { get; set; }
        public string DiscordId { get; set; }
        public string DiscordUsername { get; set; }
        public string AssignerId { get; set; }
        public string AssignerUsername { get; set; }
        public int Points { get; set; }
        public DateTime InsertedAt { get; set; }
        public DateTime? RemovedAt { get; set; }
        public string RemovedBy { get; set; }
        public string Message { get; set; }
    }
}

