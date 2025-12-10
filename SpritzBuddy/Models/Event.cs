namespace SpritzBuddy.Models
{
    public class Event
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string? Location { get; set; }
        public int OrganizerId { get; set; }
        public required ApplicationUser Organizer { get; set; }
        public virtual ICollection<EventParticipant> Participants { get; set; } = new List<EventParticipant>();
    }
}
