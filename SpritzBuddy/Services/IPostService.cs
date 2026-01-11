using SpritzBuddy.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpritzBuddy.Services
{
    public interface IPostService
    {
        /// <summary>
        /// Retrieves a paginated list of posts based on filter criteria.
        /// </summary>
        /// <param name="currentUserId">The ID of the currently logged-in user (nullable).</param>
        /// <param name="searchString">Optional search term for filtering posts.</param>
        /// <param name="pageIndex">The current page number (1-based).</param>
        /// <param name="pageSize">Number of posts per page.</param>
        /// <param name="showOnlyFollowing">If true, shows only posts from followed users (and public posts depending on logic).</param>
        /// <param name="showOnlyMyPosts">If true, shows only posts belonging to the current user.</param>
        /// <returns>A tuple containing the list of posts and total count of pages.</returns>
        Task<(List<Post> Posts, int TotalPages)> GetPostsPaginatedAsync(
            int? currentUserId,
            string searchString,
            int pageIndex,
            int pageSize,
            bool showOnlyFollowing,
            bool showOnlyMyPosts);

        /// <summary>
        /// Toggles the like status for a post. If the user already liked it, removes the like. Otherwise, adds a like.
        /// </summary>
        Task<int> ToggleLikeAsync(int postId, int userId);
    }
}
