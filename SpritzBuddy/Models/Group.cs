using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpritzBuddy.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        public DateTime CreationDate { get; set; } = DateTime.Now;

        [Required]
        public int ModeratorId { get; set; }
        public ApplicationUser Moderator { get; set; }

        public ICollection<UserGroup> Members { get; set; } = new List<UserGroup>();
        public ICollection<GroupMessage> Messages { get; set; } = new List<GroupMessage>();
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
