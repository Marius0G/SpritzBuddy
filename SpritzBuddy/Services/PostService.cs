using Microsoft.EntityFrameworkCore;
using SpritzBuddy.Data;
using SpritzBuddy.Models;
using System;
using System.Collections.Generic;
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

        public async Task<(List<Post> Posts, int TotalPages)> GetPostsPaginatedAsync(
            int? currentUserId,
            string searchString,
            int pageIndex,
            int pageSize,
            bool showOnlyFollowing,
            bool showOnlyMyPosts)
        {
            var query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.PostMedias)
                .Include(p => p.PostDrinks)
                    .ThenInclude(pd => pd.Drink)
                .Include(p => p.Likes) // Need likes for count
                .Include(p => p.Comments) // Need comments for search
                .AsQueryable();

            // 1. Filtering logic (Moved from Controller)
            if (showOnlyMyPosts && currentUserId.HasValue)
            {
                query = query.Where(p => p.UserId == currentUserId.Value);
            }
            else if (currentUserId.HasValue)
            {
                // Logic: Public posts + Posts from followed users + Own posts
                // OR just Followed + Own if that was the specific intent, but based on Controller "Index":
                // it was: !p.User.IsPrivate OR p.UserId == currentUserId OR followingIds.Contains(p.UserId)
                
                var followingIds = await _context.Follows
                    .Where(f => f.FollowerId == currentUserId.Value && f.Status == FollowStatus.Accepted)
                    .Select(f => f.FollowingId)
                    .ToListAsync();

                // Note: We need to pull followingIds into memory first as EF Core might struggle with complex local collection queries if not careful,
                // but Contains(id) usually works fine.
                
                query = query.Where(p =>
                    !p.User.IsPrivate ||                           
                    p.UserId == currentUserId.Value ||             
                    followingIds.Contains(p.UserId)                
                );
            }
            else
            {
                // Not logged in: Show only public posts
                query = query.Where(p => !p.User.IsPrivate);
            }

            // 2. Search Logic (from Course 10 techniques)
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();
                // Search in Title, Content, or Comments
                // Note: Searching in comments involves a sub-query or join logic which can be heavy.
                // Course 10 suggests searching in comments too.
                
                query = query.Where(p => 
                    p.Title.Contains(searchString) || 
                    p.Content.Contains(searchString) ||
                    p.Comments.Any(c => c.Content.Contains(searchString))
                );
            }

            // 3. Sorting
            // Default sort: Most likes, then newest
            query = query.OrderByDescending(p => p.Likes.Count)
                         .ThenByDescending(p => p.CreateDate);

            // 4. Pagination
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Ensure pageIndex is valid
            if (pageIndex < 1) pageIndex = 1;
            // if (pageIndex > totalPages && totalPages > 0) pageIndex = totalPages; // Optional: auto-correct to last page

            var posts = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (posts, totalPages);
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
