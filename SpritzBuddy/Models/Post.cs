using System.ComponentModel.DataAnnotations;

namespace SpritzBuddy.Models
{
    public class Post
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        [Required]
        [MaxLength(150)]
        public required string Title { get; set; }

        [Required]
        [MaxLength(4000)]
        public required string Content { get; set; }
        public DateTime CreateDate { get; set; }
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<PostDrink> PostDrinks { get; set; } = new List<PostDrink>();
        public virtual ICollection<PostMedia> PostMedias { get; set; } = new List<PostMedia>();
        public virtual ICollection<Like> Likes { get; set; } = new List<Like>();

    }
}
