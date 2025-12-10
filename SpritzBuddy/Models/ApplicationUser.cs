using Microsoft.AspNetCore.Identity;

namespace SpritzBuddy.Models
{
    // Moștenim IdentityUser pentru a avea deja funcționalități de login, parolă, email etc.
    public class ApplicationUser : IdentityUser<int>
    {
        // Aici vom adăuga mai târziu: NumeComplet, Descriere, Poza, etc.
        // Momentan o lăsăm goală, dar moștenește tot ce trebuie.

        public string ProfilePictureUrl { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Description { get; set; }
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