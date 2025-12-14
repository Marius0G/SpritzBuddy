using System.ComponentModel.DataAnnotations;

namespace SpritzBuddy.Models
{
    public class PostMedia
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public int OrderIndex { get; set; } // Pentru a păstra ordinea media în postare
        [Required]
        [MaxLength(1024)]
        public required string FilePath { get; set; }
        public PostMediaType MediaType { get; set; } = PostMediaType.Image; // Tipul media (imagine, video etc.)
        public virtual Post Post { get; set; } = null!;
    }
}
