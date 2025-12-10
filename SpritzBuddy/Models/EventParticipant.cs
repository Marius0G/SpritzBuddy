namespace SpritzBuddy.Models
{
    public class EventParticipant
    {
        public int UserId { get; set; }
        public int EventId { get; set; }
        public required ApplicationUser User { get; set; }
        public required Event Event { get; set; }
        public string? Status { get; set; } // e.g., "Going", "Interested", "Not Going"
    }
}
