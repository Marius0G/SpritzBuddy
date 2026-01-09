using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpritzBuddy.Models;

namespace SpritzBuddy.Data
{
    // ATENȚIE: Moștenim IdentityDbContext<ApplicationUser>, nu simplul DbContext!
    // Asta configurează automat tabelele pentru useri.
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Like> Likes => Set<Like>();
        public DbSet<PostMedia> PostMedias => Set<PostMedia>();
        public DbSet<Drink> Drinks => Set<Drink>();
        public DbSet<PostDrink> PostDrinks => Set<PostDrink>();
        public DbSet<Follow> Follows => Set<Follow>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();
        public DbSet<Group> Groups => Set<Group>();
        public DbSet<UserGroup> UserGroups => Set<UserGroup>();
        public DbSet<GroupMessage> GroupMessages => Set<GroupMessage>();
        public DbSet<GroupInvite> GroupInvites => Set<GroupInvite>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Post>()
                .HasOne(p => p.User)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PostMedia>()
                .HasOne(pm => pm.Post)
                .WithMany(p => p.PostMedias)
                .HasForeignKey(pm => pm.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<PostMedia>()
                .Property(pm => pm.MediaType)
                .HasConversion<string>();

            builder.Entity<Like>()
                .HasKey(l => new { l.PostId, l.UserId });

            builder.Entity<Like>()
                .HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PostDrink>()
                .HasKey(pd => new { pd.PostId, pd.DrinkId });

            builder.Entity<PostDrink>()
                .HasOne(pd => pd.Post)
                .WithMany(p => p.PostDrinks)
                .HasForeignKey(pd => pd.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PostDrink>()
                .HasOne(pd => pd.Drink)
                .WithMany(d => d.PostDrinks)
                .HasForeignKey(pd => pd.DrinkId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Follow>()
                .HasKey(f => new { f.FollowerId, f.FollowingId });

            builder.Entity<Follow>()
                .HasOne(f => f.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Follow>()
                .HasOne(f => f.Following)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Follow>()
                .Property(f => f.Status)
                .HasConversion<string>();

            builder.Entity<Event>()
                .HasOne(e => e.Organizer)
                .WithMany(u => u.Events)
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Event>()
                .HasOne(e => e.Group)
                .WithMany(g => g.Events)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EventParticipant>()
                .HasKey(ep => new { ep.UserId, ep.EventId });

            builder.Entity<EventParticipant>()
                .HasOne(ep => ep.User)
                .WithMany(u => u.EventParticipants)
                .HasForeignKey(ep => ep.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EventParticipant>()
                .HasOne(ep => ep.Event)
                .WithMany(e => e.Participants)
                .HasForeignKey(ep => ep.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<EventParticipant>()
                .Property(ep => ep.Status)
                .HasConversion<string>();

            builder.Entity<Group>()
                .HasOne(g => g.Moderator)
                .WithMany()
                .HasForeignKey(g => g.ModeratorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserGroup>()
                .HasKey(ug => new { ug.UserId, ug.GroupId });

            builder.Entity<UserGroup>()
                .HasOne(ug => ug.User)
                .WithMany()
                .HasForeignKey(ug => ug.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserGroup>()
                .HasOne(ug => ug.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(ug => ug.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GroupMessage>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.Messages)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GroupMessage>()
                .HasOne(gm => gm.User)
                .WithMany()
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GroupInvite>()
                .HasOne(gi => gi.Group)
                .WithMany()
                .HasForeignKey(gi => gi.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GroupInvite>()
                .HasOne(gi => gi.Inviter)
                .WithMany()
                .HasForeignKey(gi => gi.InviterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GroupInvite>()
                .HasOne(gi => gi.InvitedUser)
                .WithMany()
                .HasForeignKey(gi => gi.InvitedUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
