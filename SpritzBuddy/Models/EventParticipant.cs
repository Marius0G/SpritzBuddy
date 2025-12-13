namespace SpritzBuddy.Models
{
    public class EventParticipant
    {
        public int UserId { get; set; }
        public int EventId { get; set; }
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Event Event { get; set; } = null!;
        public EventParticipantStatus Status { get; set; } = EventParticipantStatus.Interested;
    }
}
