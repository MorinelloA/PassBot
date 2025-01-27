namespace PassBot.Models
{
    public class DBBackup
    {
        public List<CheckInLog> checkInLogs { get; set; }
        public List<Item> items { get; set; }
        public List<Log> logs { get; set; }
        public List<ProfileChangeLog> profileChangeLogs { get; set; }
        public List<Redemption> redemptions { get; set; }
        public List<UserPoints> userPoints { get; set; }
        public List<UserPointsLog> userPointsTableLogs { get; set; }
        public List<UserProfile> userProfiles { get; set; }
    }
}