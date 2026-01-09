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

            // Get upcoming events from user's groups
            var userGroupIds = await _context.UserGroups
                .Where(ug => ug.UserId == user.Id && ug.IsAccepted)
                .Select(ug => ug.GroupId)
                .ToListAsync();

            var upcomingEvents = await _context.GroupEvents
                .Include(e => e.Group)
                .Include(e => e.Creator)
                .Include(e => e.Participants)
                .Where(e => userGroupIds.Contains(e.GroupId) && e.EventDate > DateTime.Now)
                .OrderBy(e => e.EventDate)
                .Take(10)
                .ToListAsync();

            ViewBag.FollowRequests = followRequests;
            ViewBag.GroupInvites = groupInvites;
            ViewBag.UpcomingEvents = upcomingEvents;
            ViewBag.CurrentUserId = user.Id;
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

            // Get upcoming events count (events in next 7 days that user hasn't responded to)
            var userGroupIds = await _context.UserGroups
                .Where(ug => ug.UserId == user.Id && ug.IsAccepted)
                .Select(ug => ug.GroupId)
                .ToListAsync();

            var upcomingEventsCount = await _context.GroupEvents
                .Where(e => userGroupIds.Contains(e.GroupId) && 
                           e.EventDate > DateTime.Now && 
                           e.EventDate <= DateTime.Now.AddDays(7) &&
                           !e.Participants.Any(p => p.UserId == user.Id))
                .CountAsync();

            var totalCount = followRequestsCount + groupInvitesCount + upcomingEventsCount;

            return Json(new { 
                count = totalCount,
                followRequests = followRequestsCount,
                groupInvites = groupInvitesCount,
                upcomingEvents = upcomingEventsCount
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationsHtml()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Content("<div class='alert alert-warning'>Please log in to view notifications.</div>");

            // Get follow requests
            var followRequests = await _context.Follows
                .Include(f => f.Follower)
                .Where(f => f.FollowingId == user.Id && f.Status == FollowStatus.Pending)
                .OrderByDescending(f => f.RequestDate)
                .ToListAsync();

            // Get group invites
            var groupInvites = await _groupService.GetPendingInvitesForUserAsync(user.Id);

            // Get upcoming events from user's groups
            var userGroupIds = await _context.UserGroups
                .Where(ug => ug.UserId == user.Id && ug.IsAccepted)
                .Select(ug => ug.GroupId)
                .ToListAsync();

            var upcomingEvents = await _context.GroupEvents
                .Include(e => e.Group)
                .Include(e => e.Creator)
                .Include(e => e.Participants)
                .Where(e => userGroupIds.Contains(e.GroupId) && e.EventDate > DateTime.Now)
                .OrderBy(e => e.EventDate)
                .Take(10)
                .ToListAsync();

            ViewBag.FollowRequests = followRequests;
            ViewBag.GroupInvites = groupInvites;
            ViewBag.UpcomingEvents = upcomingEvents;
            ViewBag.CurrentUserId = user.Id;

            return PartialView("_NotificationsPartial");
        }
    }
}
