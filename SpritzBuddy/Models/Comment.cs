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

        // CAMPURI NOI PENTRU ANALIZA DE SENTIMENT
        // Eticheta sentimentului: "positive", "neutral", "negative"
        public string? SentimentLabel { get; set; }
        // Scorul de incredere: valoare intre 0.0 si 1.0
        public double? SentimentConfidence { get; set; }
        // Data si ora la care s-a efectuat analiza
        public DateTime? SentimentAnalyzedAt { get; set; }

        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Post Post { get; set; } = null!;
    }
}
