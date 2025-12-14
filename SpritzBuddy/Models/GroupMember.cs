namespace SpritzBuddy.Models
{
    public class GroupMember
    {
        public int UserId { get; set; }
        public int GroupId { get; set; }

        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Group Group { get; set; } = null!;
        public DateTime JoinedDate { get; set; }
    }
}
