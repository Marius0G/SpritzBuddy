using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SpritzBuddy.Models
{
    // Moștenim IdentityUser pentru a avea deja funcționalități de login, parolă, email etc.
    public class ApplicationUser : IdentityUser<int>
    {
        [MaxLength(512)]
        [DataType(DataType.ImageUrl)]
        public string? ProfilePictureUrl { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsPrivate { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime LastActiveDate { get; set; }

        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
        public virtual ICollection<Follow> Followers { get; set; } = new List<Follow>();
        public virtual ICollection<Follow> Following { get; set; } = new List<Follow>();
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
        public virtual ICollection<EventParticipant> EventParticipants { get; set; } = new List<EventParticipant>();
        public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
        public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
        public virtual ICollection<GroupMessage> GroupMessages { get; set; } = new List<GroupMessage>();
    }
}
