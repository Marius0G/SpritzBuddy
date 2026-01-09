using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpritzBuddy.Data;
using SpritzBuddy.Models;
using SpritzBuddy.Services;

namespace SpritzBuddy.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGroupService _groupService;

        public NotificationsController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            IGroupService groupService)
        {
            _context = context;
            _userManager = userManager;
            _groupService = groupService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Get follow requests
            var followRequests = await _context.Follows
                .Include(f => f.Follower)
                .Where(f => f.FollowingId == user.Id && f.Status == FollowStatus.Pending)
                .OrderByDescending(f => f.RequestDate)
                .ToListAsync();

            // Get group invites
            var groupInvites = await _groupService.GetPendingInvitesForUserAsync(user.Id);

            ViewBag.FollowRequests = followRequests;
            ViewBag.GroupInvites = groupInvites;
            ViewBag.TotalCount = followRequests.Count + groupInvites.Count;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { count = 0 });

            var followRequestsCount = await _context.Follows
                .CountAsync(f => f.FollowingId == user.Id && f.Status == FollowStatus.Pending);

            var groupInvites = await _groupService.GetPendingInvitesForUserAsync(user.Id);
            var groupInvitesCount = groupInvites.Count;

            var totalCount = followRequestsCount + groupInvitesCount;

            return Json(new { 
                count = totalCount,
                followRequests = followRequestsCount,
                groupInvites = groupInvitesCount
            });
        }
    }
}
