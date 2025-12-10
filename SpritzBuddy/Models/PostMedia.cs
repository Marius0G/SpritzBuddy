namespace SpritzBuddy.Models
{
    public class PostMedia
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public int OrderIndex { get; set; } // Pentru a păstra ordinea media în postare
        public required string FilePath { get; set; }
        public required string MediaType { get; set; } // e.g., "image", "video"
        public required Post Post { get; set; }
    }
}
