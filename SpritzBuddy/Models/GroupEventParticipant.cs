using System;
using System.ComponentModel.DataAnnotations;

namespace SpritzBuddy.Models
{
    public enum EventParticipationStatus
    {
        Going,      // Vine
        NotGoing,   // Nu vine
        Maybe       // Poate
    }

    public class GroupEventParticipant
    {
        [Required]
        public int EventId { get; set; }
        public GroupEvent Event { get; set; }

        [Required]
        public int UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        public EventParticipationStatus Status { get; set; }

        public DateTime ResponseDate { get; set; } = DateTime.Now;
    }
}
