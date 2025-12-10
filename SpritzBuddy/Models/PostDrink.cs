namespace SpritzBuddy.Models
{
    public class PostDrink
    {
        public int PostId { get; set; }
        public int DrinkId { get; set; }
        public required Post Post { get; set; }
        public required Drink Drink { get; set; }
    }
}
