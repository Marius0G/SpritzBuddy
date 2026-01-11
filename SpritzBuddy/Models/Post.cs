using System.ComponentModel.DataAnnotations;

namespace SpritzBuddy.Models
{
    public class Post
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        [Required(ErrorMessage = "Titlul este obligatoriu.")]
        [MaxLength(150, ErrorMessage = "Titlul nu poate depăși 150 de caractere.")]
        [MinLength(5, ErrorMessage = "Titlul trebuie să aibă cel puțin 5 caractere.")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "Conținutul este obligatoriu.")]
        [MaxLength(4000, ErrorMessage = "Conținutul nu poate depăși 4000 de caractere.")]
        public required string Content { get; set; }
        public DateTime CreateDate { get; set; }
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<PostDrink> PostDrinks { get; set; } = new List<PostDrink>();
        public virtual ICollection<PostMedia> PostMedias { get; set; } = new List<PostMedia>();
        public virtual ICollection<Like> Likes { get; set; } = new List<Like>();

    }
}
