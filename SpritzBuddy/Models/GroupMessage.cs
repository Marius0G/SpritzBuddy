namespace SpritzBuddy.Models
{
    public class GroupMessage
    {
        public int Id { get; set; }
        public required int GroupId { get; set; }
        public required int SenderId { get; set; }
        public required string Content { get; set; }
        public DateTime SentDate { get; set; }
        public required Group Group { get; set; }
        public required ApplicationUser Sender { get; set; }
    }
}
