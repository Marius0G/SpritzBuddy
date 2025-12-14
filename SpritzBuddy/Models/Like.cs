namespace SpritzBuddy.Models
{
    public class Like
    {
        public int PostId { get; set; }
        public int UserId { get; set; }
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Post Post { get; set; } = null!;

    }
}
