using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpritzBuddy.Data;
using SpritzBuddy.Models;
using System.Security.Claims;

namespace SpritzBuddy.Controllers
{
    [Authorize]
    public class FollowsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FollowsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Follows/Requests - View all pending follow requests received
        public async Task<IActionResult> Requests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get all pending follow requests WHERE current user is being followed
            var pendingRequests = await _context.Follows
                .Include(f => f.Follower)
                .Where(f => f.FollowingId == userIdInt && f.Status == FollowStatus.Pending)
                .OrderByDescending(f => f.RequestDate)
                .ToListAsync();

            return View(pendingRequests);
        }

        // GET: Follows/Followers - View all approved followers
        public async Task<IActionResult> Followers(int? userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? currentUserIdInt = null;
            if (currentUserId != null && int.TryParse(currentUserId, out int parsedId))
            {
                currentUserIdInt = parsedId;
            }

            int targetUserId = userId ?? currentUserIdInt ?? 0;
            if (targetUserId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var followers = await _context.Follows
                .Include(f => f.Follower)
                .Where(f => f.FollowingId == targetUserId && f.Status == FollowStatus.Accepted)
                .OrderByDescending(f => f.RequestDate)
                .ToListAsync();

            var targetUser = await _context.ApplicationUsers.FindAsync(targetUserId);
            ViewBag.TargetUser = targetUser;
            ViewBag.IsOwnProfile = targetUserId == currentUserIdInt;

            return View(followers);
        }

        // GET: Follows/Following - View all users you're following
        public async Task<IActionResult> Following(int? userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? currentUserIdInt = null;
            if (currentUserId != null && int.TryParse(currentUserId, out int parsedId))
            {
                currentUserIdInt = parsedId;
            }

            int targetUserId = userId ?? currentUserIdInt ?? 0;
            if (targetUserId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var following = await _context.Follows
                .Include(f => f.Following)
                .Where(f => f.FollowerId == targetUserId && f.Status == FollowStatus.Accepted)
                .OrderByDescending(f => f.RequestDate)
                .ToListAsync();

            var targetUser = await _context.ApplicationUsers.FindAsync(targetUserId);
            ViewBag.TargetUser = targetUser;
            ViewBag.IsOwnProfile = targetUserId == currentUserIdInt;

            return View(following);
        }

        // POST: Follows/SendRequest - Send a follow request to a user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRequest(int followingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            // Prevent following yourself
            if (userIdInt == followingId)
            {
                return Json(new { success = false, message = "You cannot follow yourself" });
            }

            // Check if user to follow exists
            var userToFollow = await _context.ApplicationUsers.FindAsync(followingId);
            if (userToFollow == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Check if follow relationship already exists
            var existingFollow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == userIdInt && f.FollowingId == followingId);

            if (existingFollow != null)
            {
                if (existingFollow.Status == FollowStatus.Pending)
                {
                    return Json(new { success = false, message = "Follow request already sent" });
                }
                if (existingFollow.Status == FollowStatus.Accepted)
                {
                    return Json(new { success = false, message = "Already following this user" });
                }
                // If rejected, allow sending a new request
                existingFollow.Status = FollowStatus.Pending;
                existingFollow.RequestDate = DateTime.UtcNow;
                _context.Follows.Update(existingFollow);
            }
            else
            {
                // Create new follow request
                var follow = new Follow
                {
                    FollowerId = userIdInt,
                    FollowingId = followingId,
                    Status = FollowStatus.Pending,
                    RequestDate = DateTime.UtcNow
                };

                _context.Follows.Add(follow);
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = userToFollow.IsPrivate
                    ? "Follow request sent! Waiting for approval."
                    : "You are now following this user!",
                status = "Pending"
            });
        }

        // POST: Follows/Approve - Approve a follow request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int followerId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == userIdInt);

            if (follow == null)
            {
                return Json(new { success = false, message = "Follow request not found" });
            }

            if (follow.Status != FollowStatus.Pending)
            {
                return Json(new { success = false, message = "This request has already been processed" });
            }

            follow.Status = FollowStatus.Accepted;
            _context.Follows.Update(follow);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Follow request approved" });
        }

        // POST: Follows/Reject - Reject a follow request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int followerId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == userIdInt);

            if (follow == null)
            {
                return Json(new { success = false, message = "Follow request not found" });
            }

            if (follow.Status != FollowStatus.Pending)
            {
                return Json(new { success = false, message = "This request has already been processed" });
            }

            follow.Status = FollowStatus.Rejected;
            _context.Follows.Update(follow);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Follow request rejected" });
        }

        // POST: Follows/Unfollow - Unfollow a user or cancel follow request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unfollow(int followingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == userIdInt && f.FollowingId == followingId);

            if (follow == null)
            {
                return Json(new { success = false, message = "You are not following this user" });
            }

            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();

            var message = follow.Status == FollowStatus.Pending
                ? "Follow request cancelled"
                : "Unfollowed successfully";

            return Json(new { success = true, message = message });
        }

        // POST: Follows/RemoveFollower - Remove a follower
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFollower(int followerId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || !int.TryParse(userId, out int userIdInt))
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == userIdInt);

            if (follow == null)
            {
                return Json(new { success = false, message = "Follower not found" });
            }

            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Follower removed" });
        }

        // GET: Follows/GetFollowStatus - Get follow status for a user (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetFollowStatus(int userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null || !int.TryParse(currentUserId, out int currentUserIdInt))
            {
                return Json(new { success = true, isFollowing = false, status = "NotAuthenticated" });
            }

            if (currentUserIdInt == userId)
            {
                return Json(new { success = true, isFollowing = false, status = "Self" });
            }

            var follow = await _context.Follows
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserIdInt && f.FollowingId == userId);

            if (follow == null)
            {
                return Json(new { success = true, isFollowing = false, status = "NotFollowing" });
            }

            return Json(new
            {
                success = true,
                isFollowing = follow.Status == FollowStatus.Accepted,
                status = follow.Status.ToString(),
                isPending = follow.Status == FollowStatus.Pending
            });
        }

        // GET: Follows/GetFollowCounts - Get follower and following counts (AJAX)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetFollowCounts(int userId)
        {
            var followersCount = await _context.Follows
                .CountAsync(f => f.FollowingId == userId && f.Status == FollowStatus.Accepted);

            var followingCount = await _context.Follows
                .CountAsync(f => f.FollowerId == userId && f.Status == FollowStatus.Accepted);

            var pendingRequestsCount = await _context.Follows
                .CountAsync(f => f.FollowingId == userId && f.Status == FollowStatus.Pending);

            return Json(new
            {
                success = true,
                followersCount = followersCount,
                followingCount = followingCount,
                pendingRequestsCount = pendingRequestsCount
            });
        }
    }
}
