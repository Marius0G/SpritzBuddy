using System.ComponentModel.DataAnnotations;

namespace SpritzBuddy.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }

        [Required]
        [MaxLength(1000)]
        public required string Content { get; set; }
        public DateTime CreateDate { get; set; }

        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Post Post { get; set; } = null!;
    }
}
