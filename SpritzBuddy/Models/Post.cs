namespace SpritzBuddy.Models
{
    public class Post
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public DateTime CreateDate { get; set; }
        public required ApplicationUser User { get; set; }
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<PostDrink> PostDrinks { get; set; } = new List<PostDrink>();
        public ICollection<PostMedia> PostMedias { get; set; } = new List<PostMedia>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();

    }
}
