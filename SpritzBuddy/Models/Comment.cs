using System.ComponentModel.DataAnnotations;

namespace SpritzBuddy.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }

        public required string Content { get; set; }
        public DateTime CreateDate { get; set; }

        public required ApplicationUser User { get; set; }
        public required Post Post { get; set; }
    }
}
