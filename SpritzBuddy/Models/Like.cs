namespace SpritzBuddy.Models
{
    public class Like
    {
        public int PostId { get; set; }
        public int UserId { get; set; }
        public required ApplicationUser User { get; set; }
        public required Post Post { get; set; }

    }
}
