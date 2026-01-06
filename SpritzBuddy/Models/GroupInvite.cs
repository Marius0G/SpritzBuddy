using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpritzBuddy.Models
{
    public class GroupInvite
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GroupId { get; set; }
        public Group Group { get; set; }

        [Required]
        public int InviterId { get; set; } // Cine a trimis invite-ul (moderator)
        public ApplicationUser Inviter { get; set; }

        [Required]
        public int InvitedUserId { get; set; } // Cine primește invite-ul
        public ApplicationUser InvitedUser { get; set; }

        public DateTime InvitedDate { get; set; } = DateTime.Now;

        public bool IsAccepted { get; set; } = false;
        public bool IsDeclined { get; set; } = false;
    }
}
