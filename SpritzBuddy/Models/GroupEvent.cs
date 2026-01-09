using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SpritzBuddy.Models
{
    public class GroupEvent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        public string? Location { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required]
        public int GroupId { get; set; }
        public Group Group { get; set; }

        [Required]
        public int CreatorId { get; set; }
        public ApplicationUser Creator { get; set; }

        public ICollection<GroupEventParticipant> Participants { get; set; } = new List<GroupEventParticipant>();
    }
}
