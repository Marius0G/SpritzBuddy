using System.ComponentModel.DataAnnotations;

namespace SpritzBuddy.Models
{
    public class Group
    {
        public int Id { get; set; }
        public required int AdminId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public virtual ApplicationUser Admin { get; set; } = null!;
        public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
        public virtual ICollection<GroupMessage> GroupMessages { get; set; } = new List<GroupMessage>();
    }
}
