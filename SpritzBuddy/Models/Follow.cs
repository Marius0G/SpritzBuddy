namespace SpritzBuddy.Models
{
    public class Follow
    {
        public int FollowerId { get; set; }
        public int FollowingId { get; set; }
        public required ApplicationUser Follower { get; set; }
        public required ApplicationUser Following { get; set; }
        public string Status { get; set; }
        public DateTime RequestDate { get; set; }
    }

}
