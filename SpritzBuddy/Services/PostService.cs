using Microsoft.EntityFrameworkCore;
using SpritzBuddy.Data;
using SpritzBuddy.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SpritzBuddy.Services
{
    public class PostService : IPostService
    {
        private readonly ApplicationDbContext _context;

        public PostService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Toggles the like status for a post. If the user already liked it, removes the like. Otherwise, adds a like.
        /// Returns the new like count for the post.
        /// </summary>
        public async Task<int> ToggleLikeAsync(int postId, int userId)
        {
            // Check if the like already exists
            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (existingLike != null)
            {
                // Unlike: Remove the existing like
                _context.Likes.Remove(existingLike);
            }
            else
            {
                // Like: Create a new like
                var newLike = new Like
                {
                    PostId = postId,
                    UserId = userId
                };
                _context.Likes.Add(newLike);
            }

            await _context.SaveChangesAsync();

            // Return the new like count
            var likeCount = await _context.Likes.CountAsync(l => l.PostId == postId);
            return likeCount;
        }
    }
}
