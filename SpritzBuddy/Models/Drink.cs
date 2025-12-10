namespace SpritzBuddy.Models
{
    public class Drink
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        
        public string? ColorHex { get; set; }
        public string? Description { get; set; }
        public decimal AlcoholContent { get; set; } // Procentul de alcool

        public string? PicturePath { get; set; }
        public ICollection<PostDrink> PostDrinks { get; set; } = new List<PostDrink>();
    }
}
