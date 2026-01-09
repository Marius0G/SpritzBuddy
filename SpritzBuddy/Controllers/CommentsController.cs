using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpritzBuddy.Data;
using SpritzBuddy.Models;
using SpritzBuddy.Services;
using System.Security.Claims;

namespace SpritzBuddy.Controllers
{
    [Authorize] // Only logged-in users can comment
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IContentModerationService _moderationService;

        public CommentsController(ApplicationDbContext context, IContentModerationService moderationService)
        {
            _context = context;
            _moderationService = moderationService;
        }

        // POST: Comments/Create - Add a comment to a post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int postId, string content)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "User not authenticated" });
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Comment cannot be empty" });
                TempData["Error"] = "Comment cannot be empty";
                return RedirectToAction("Index", "Home");
            }

            // AI Content Moderation
            if (!await _moderationService.IsContentSafeAsync(content))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Conținutul tău conține termeni nepotriviți. Te rugăm să reformulezi." });
                
                TempData["Error"] = "Conținutul tău conține termeni nepotriviți. Te rugăm să reformulezi.";
                return RedirectToAction("PostComments", new { id = postId });
            }

            // Check if post exists
            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Post not found" });
                TempData["Error"] = "Post not found";
                return RedirectToAction("Index", "Home");
            }

            var comment = new Comment
            {
                PostId = postId,
                UserId = userIdInt,
                Content = content.Trim(),
                CreateDate = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Load user data for the response
            var user = await _context.ApplicationUsers.FindAsync(userIdInt);

            // Handle AJAX requests
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    comment = new
                    {
                        id = comment.Id,
                        content = comment.Content,
                        createDate = comment.CreateDate.ToString("MMM dd, yyyy HH:mm"),
                        userName = $"{user?.FirstName} {user?.LastName}",
                        userId = userIdInt
                    }
                });
            }

            // Handle form post - redirect back to home
            TempData["Success"] = "Comment added!";
            
            // Check if there's a return URL or redirect to post comments page
            var returnUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains("/Comments/PostComments/"))
            {
                return RedirectToAction("PostComments", new { id = postId });
            }
            
            return RedirectToAction("Index", "Home");
        }

        // GET: Comments/PostComments/5 - Get all comments for a post (AJAX)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPostComments(int postId)
        {
            var comments = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.PostId == postId)
                .OrderByDescending(c => c.CreateDate)
                .Select(c => new
                {
                    id = c.Id,
                    content = c.Content,
                    createDate = c.CreateDate.ToString("MMM dd, yyyy HH:mm"),
                    userName = $"{c.User.FirstName} {c.User.LastName}",
                    userId = c.UserId
                })
                .ToListAsync();

            var currentUserId = 0;
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdString != null && int.TryParse(userIdString, out int parsedUserId))
            {
                currentUserId = parsedUserId;
            }

            return Json(new
            {
                success = true,
                comments = comments,
                currentUserId = currentUserId
            });
        }

        // POST: Comments/Delete/5 - Delete a comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return Json(new { success = false, message = "Comment not found" });
            }

            // Check if user owns this comment or is admin
            var isAdmin = User.IsInRole("Administrator");
            if (!isAdmin && comment.UserId != userIdInt)
            {
                return Json(new { success = false, message = "You can only delete your own comments" });
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Comment deleted successfully" });
        }

        // GET: Comments/PostComments/5 - Show all comments for a post (View)
        [AllowAnonymous]
        public async Task<IActionResult> PostComments(int id)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.PostMedias)
                .Include(p => p.Likes)
                .Include(p => p.PostDrinks)
                    .ThenInclude(pd => pd.Drink)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            var comments = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.PostId == id)
                .OrderBy(c => c.CreateDate)
                .ToListAsync();

            // Get all posts from the same user with media
            var userPosts = await _context.Posts
                .Include(p => p.PostMedias)
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .Where(p => p.UserId == post.UserId && p.PostMedias.Any())
                .OrderByDescending(p => p.CreateDate)
                .ToListAsync();

            ViewBag.Post = post;
            ViewBag.UserPosts = userPosts;
            ViewBag.CurrentPostIndex = userPosts.FindIndex(p => p.Id == id);
            
            return View(comments);
        }

        // POST: Comments/Edit/5 - Edit a comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string content)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Comment cannot be empty" });
            }

            // AI Content Moderation
            if (!await _moderationService.IsContentSafeAsync(content))
            {
                return Json(new { success = false, message = "Conținutul tău conține termeni nepotriviți. Te rugăm să reformulezi." });
            }

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return Json(new { success = false, message = "Comment not found" });
            }

            // Check if user owns this comment
            if (comment.UserId != userIdInt)
            {
                return Json(new { success = false, message = "You can only edit your own comments" });
            }

            comment.Content = content.Trim();
            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                comment = new
                {
                    id = comment.Id,
                    content = comment.Content,
                    createDate = comment.CreateDate.ToString("MMM dd, yyyy HH:mm")
                }
            });
        }
    }
}
