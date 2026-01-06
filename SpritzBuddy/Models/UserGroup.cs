using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpritzBuddy.Models
{
    public class UserGroup
    {

        [Required]
        public int UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        public int GroupId { get; set; }
        public Group Group { get; set; }

        public DateTime JoinedDate { get; set; } = DateTime.Now;
        public bool IsAccepted { get; set; } = false;
    }
}
