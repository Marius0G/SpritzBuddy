using System.ComponentModel.DataAnnotations;

namespace SpritzBuddy.Models
{
    public class Event
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(150)]
        public required string Title { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public DateTime EventDate { get; set; }
        [MaxLength(250)]
        public string? Location { get; set; }
        public int OrganizerId { get; set; }
        public virtual ApplicationUser Organizer { get; set; } = null!;
        public virtual ICollection<EventParticipant> Participants { get; set; } = new List<EventParticipant>();
    }
}
