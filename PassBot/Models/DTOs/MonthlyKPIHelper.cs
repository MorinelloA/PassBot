namespace PassBot.Models
{
    public class MonthlyKPIHelper
    {
        public int month { get; set; }
        public int year { get; set; }
        public int totalUsers { get; set; }
        public int activeUsers { get; set; }
        public int pointsDistributed { get; set; }
        public int actions { get; set; }
        public int polls { get; set; }
        public int pollResponses { get; set; }
        public decimal averageNumOfPollResponses { get; set; }
    }
}

