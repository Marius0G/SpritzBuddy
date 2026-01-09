using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpritzBuddy.Data;
using SpritzBuddy.Models;
using System.Security.Claims;

namespace SpritzBuddy.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Search
        public async Task<IActionResult> Index(string query)
        {
            // Get current user ID for follow status
            var currentUserId = GetCurrentUserId();

            List<ApplicationUser> users;

            if (string.IsNullOrWhiteSpace(query))
            {
                // Show all users if no search query
                users = await _context.ApplicationUsers
                    .Where(u => u.Id != currentUserId) // Exclude current user
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Take(100) // Limit to 100 users
                    .ToListAsync();
                    
                ViewBag.Query = "";
            }
            else
            {
                var searchTerm = query.Trim().ToLower();

                // Search users by first name, last name, username, or email
                users = await _context.ApplicationUsers
                    .Where(u => 
                        u.FirstName.ToLower().Contains(searchTerm) ||
                        u.LastName.ToLower().Contains(searchTerm) ||
                        (u.UserName != null && u.UserName.ToLower().Contains(searchTerm)) ||
                        (u.Email != null && u.Email.ToLower().Contains(searchTerm))
                    )
                    .Where(u => u.Id != currentUserId) // Exclude current user
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Take(50) // Limit to 50 results
                    .ToListAsync();

                ViewBag.Query = query;
            }

            ViewBag.CurrentUserId = currentUserId;

            return View(users);
        }

        // GET: Search/Users - AJAX endpoint for autocomplete
        [HttpGet]
        public async Task<IActionResult> Users(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Json(new List<object>());
            }

            var searchTerm = term.Trim().ToLower();
            var currentUserId = GetCurrentUserId();

            var users = await _context.ApplicationUsers
                .Where(u =>
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm) ||
                    (u.UserName != null && u.UserName.ToLower().Contains(searchTerm))
                )
                .Where(u => u.Id != currentUserId)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Take(10)
                .Select(u => new
                {
                    id = u.Id,
                    userName = u.UserName,
                    fullName = u.FirstName + " " + u.LastName,
                    profilePictureUrl = u.ProfilePictureUrl,
                    isPrivate = u.IsPrivate
                })
                .ToListAsync();

            return Json(users);
        }

        // GET: Search/GetUserDetails - Get details about a specific user (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetUserDetails(int userId)
        {
            var currentUserId = GetCurrentUserId();

            var user = await _context.ApplicationUsers
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    id = u.Id,
                    userName = u.UserName,
                    fullName = u.FirstName + " " + u.LastName,
                    firstName = u.FirstName,
                    lastName = u.LastName,
                    description = u.Description,
                    profilePictureUrl = u.ProfilePictureUrl,
                    isPrivate = u.IsPrivate,
                    postsCount = u.Posts.Count,
                    followersCount = u.Followers.Count(f => f.Status == FollowStatus.Accepted),
                    followingCount = u.Following.Count(f => f.Status == FollowStatus.Accepted)
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Check follow status
            var followStatus = await _context.Follows
                .Where(f => f.FollowerId == currentUserId && f.FollowingId == userId)
                .Select(f => f.Status)
                .FirstOrDefaultAsync();

            return Json(new
            {
                success = true,
                user = user,
                followStatus = followStatus.ToString(),
                isFollowing = followStatus == FollowStatus.Accepted,
                isPending = followStatus == FollowStatus.Pending
            });
        }

        private int GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null && int.TryParse(userId, out int userIdInt))
            {
                return userIdInt;
            }
            return 0;
        }
    }
}
