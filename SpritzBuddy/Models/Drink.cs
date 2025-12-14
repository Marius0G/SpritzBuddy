using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpritzBuddy.Models
{
    public class Drink
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(7)]
        public string? ColorHex { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal AlcoholContent { get; set; } // Procentul de alcool

        [MaxLength(512)]
        public string? PicturePath { get; set; }

        public virtual ICollection<PostDrink> PostDrinks { get; set; } = new List<PostDrink>();
    }
}
