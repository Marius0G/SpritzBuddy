namespace SpritzBuddy.Models
{
    public class Group
    {
        public int Id { get; set; }
        public required int AdminId { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public required ApplicationUser Admin { get; set; }
        public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
        public virtual ICollection<GroupMessage> GroupMessages { get; set; } = new List<GroupMessage>();
    }
}
