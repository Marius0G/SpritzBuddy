namespace SpritzBuddy.Models
{
    public class Follow
    {
        public int FollowerId { get; set; }
        public int FollowingId { get; set; }
        public virtual ApplicationUser Follower { get; set; } = null!;
        public virtual ApplicationUser Following { get; set; } = null!;
        public FollowStatus Status { get; set; } = FollowStatus.Pending;
        public DateTime RequestDate { get; set; }
    }

}
