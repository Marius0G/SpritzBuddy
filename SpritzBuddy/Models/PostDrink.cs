namespace SpritzBuddy.Models
{
    public class PostDrink
    {
        public int PostId { get; set; }
        public int DrinkId { get; set; }
        public virtual Post Post { get; set; } = null!;
        public virtual Drink Drink { get; set; } = null!;
    }
}
