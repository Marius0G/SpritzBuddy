using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpritzBuddy.Data;
using SpritzBuddy.Models;
using System.Security.Claims;

namespace SpritzBuddy.Controllers
{
    [Authorize] // Only logged-in users can like posts
    public class LikesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LikesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: Likes/Toggle - Toggle like/unlike for a post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            // Check if post exists
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                return Json(new { success = false, message = "Post not found" });
            }

            // Check if user already liked this post
            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userIdInt);

            bool isLiked;
            int likeCount;

            if (existingLike != null)
            {
                // Unlike: Remove the like
                _context.Likes.Remove(existingLike);
                isLiked = false;
            }
            else
            {
                // Like: Add new like
                var newLike = new Like
                {
                    PostId = postId,
                    UserId = userIdInt
                };
                _context.Likes.Add(newLike);
                isLiked = true;
            }

            await _context.SaveChangesAsync();

            // Get updated like count
            likeCount = await _context.Likes.CountAsync(l => l.PostId == postId);

            return Json(new
            {
                success = true,
                isLiked = isLiked,
                likeCount = likeCount
            });
        }

        // POST: Likes/Like - Like a post (form post)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if post exists
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                TempData["Error"] = "Post not found";
                return RedirectToAction("Index", "Home");
            }

            // Check if user already liked this post
            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userIdInt);

            if (existingLike == null)
            {
                // Add new like
                var newLike = new Like
                {
                    PostId = postId,
                    UserId = userIdInt
                };
                _context.Likes.Add(newLike);
                await _context.SaveChangesAsync();
            }

            // Check if coming from PostComments page
            var returnUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains("/Comments/PostComments/"))
            {
                return RedirectToAction("PostComments", "Comments", new { id = postId });
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: Likes/Unlike - Unlike a post (form post)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlike(int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Account");
            }

            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userIdInt);

            if (existingLike != null)
            {
                _context.Likes.Remove(existingLike);
                await _context.SaveChangesAsync();
            }

            // Check if coming from PostComments page
            var returnUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains("/Comments/PostComments/"))
            {
                return RedirectToAction("PostComments", "Comments", new { id = postId });
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: Likes/GetLikeStatus - Get like status for a post (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetLikeStatus(int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return Json(new { success = false, isLiked = false, likeCount = 0 });
            }

            // Check if user liked this post
            var isLiked = await _context.Likes
                .AnyAsync(l => l.PostId == postId && l.UserId == userIdInt);

            // Get total like count
            var likeCount = await _context.Likes.CountAsync(l => l.PostId == postId);

            return Json(new
            {
                success = true,
                isLiked = isLiked,
                likeCount = likeCount
            });
        }

        // GET: Likes/PostLikes/5 - Show all users who liked a specific post
        public async Task<IActionResult> PostLikes(int id)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            var likes = await _context.Likes
                .Include(l => l.User)
                .Where(l => l.PostId == id)
                .OrderByDescending(l => l.User.FirstName)
                .ToListAsync();

            ViewBag.Post = post;
            return View(likes);
        }

        // GET: Likes/UserLikes - Show all posts the current user has liked
        public async Task<IActionResult> UserLikes()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Account");
            }

            var likedPosts = await _context.Likes
                .Include(l => l.Post)
                .ThenInclude(p => p.User)
                .Where(l => l.UserId == userIdInt)
                .Select(l => l.Post)
                .OrderByDescending(p => p.CreateDate)
                .ToListAsync();

            return View(likedPosts);
        }

        // POST: Likes/RemoveFromUserLikes - Remove like from user's liked posts page
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromUserLikes(int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Account");
            }

            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userIdInt);

            if (like != null)
            {
                _context.Likes.Remove(like);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Post removed from your liked posts.";
            }

            return RedirectToAction(nameof(UserLikes));
        }
    }
}
