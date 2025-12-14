using System.ComponentModel.DataAnnotations;

namespace SpritzBuddy.Models
{
    public class GroupMessage
    {
        public int Id { get; set; }
        public required int GroupId { get; set; }
        public required int SenderId { get; set; }
        [Required]
        [MaxLength(1000)]
        public required string Content { get; set; }
        public DateTime SentDate { get; set; }
        public virtual Group Group { get; set; } = null!;
        public virtual ApplicationUser Sender { get; set; } = null!;
    }
}
