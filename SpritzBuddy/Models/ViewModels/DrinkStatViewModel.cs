namespace SpritzBuddy.Models.ViewModels
{
    public class DrinkStatViewModel
    {
        public string DrinkName { get; set; } = string.Empty;
        public double Percentage { get; set; }
        public string ColorHex { get; set; } = "#6c757d"; // Default gray color
    }
}
