namespace PassBot.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public long Cost { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ExpiresOn { get; set; }
    }

}

