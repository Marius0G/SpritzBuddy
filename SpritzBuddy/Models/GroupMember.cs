namespace SpritzBuddy.Models
{
    public class GroupMember
    {
        public int UserId { get; set; }
        public int GroupId { get; set; }

        public required ApplicationUser User { get; set; }
        public required Group Group { get; set; }
        public DateTime JoinedDate { get; set; }
    }
}
