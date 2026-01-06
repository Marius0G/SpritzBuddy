using SpritzBuddy.Models;

namespace SpritzBuddy.Models.ViewModels
{
    public class GroupDetailsViewModel
    {
        public Group Group { get; set; }
        public string CurrentUserId { get; set; }
        public bool IsModerator { get; set; }
        public bool IsMember { get; set; }
        public bool IsPending { get; set; }
    }
}
