using System.Threading.Tasks;

namespace SpritzBuddy.Services
{
    public interface IPostService
    {
        /// <summary>
        /// Toggles the like status for a post. If the user already liked it, removes the like. Otherwise, adds a like.
        /// </summary>
        Task<int> ToggleLikeAsync(int postId, int userId);
    }
}
