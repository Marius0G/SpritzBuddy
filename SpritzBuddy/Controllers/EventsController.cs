using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore; // Added for Includes if needed directly, though service handles most
using SpritzBuddy.Models;
using SpritzBuddy.Services;
using System.Linq; // Added for LINQ
using System.Collections.Generic; // Added for List

namespace SpritzBuddy.Controllers
{
    [Authorize]
    public class EventsController : Controller
    {
        private readonly IGroupService _groupService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IContentModerationService _moderationService;

        public EventsController(IGroupService groupService, UserManager<ApplicationUser> userManager, IContentModerationService moderationService)
        {
            _groupService = groupService;
            _userManager = userManager;
            _moderationService = moderationService;
        }

        // GET: Events/Create?groupId=5
        [HttpGet]
        public async Task<IActionResult> Create(int groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var group = await _groupService.GetGroupWithMembersAndMessagesAsync(groupId);
            if (group == null) return NotFound();

            // Check if user is member or moderator
            var isMember = group.Members.Any(m => m.UserId == user.Id && m.IsAccepted);
            if (!isMember && group.ModeratorId != user.Id)
            {
                return Forbid();
            }

            ViewBag.GroupId = groupId;
            ViewBag.GroupName = group.Name;
            return View();
        }

        // POST: Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int groupId, string title, string description, DateTime eventDate, string? location)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Basic validation
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
            {
                ViewBag.ErrorMessage = "Titlul și descrierea sunt obligatorii.";
                ViewBag.GroupId = groupId;
                var groupForError = await _groupService.GetGroupWithMembersAndMessagesAsync(groupId);
                ViewBag.GroupName = groupForError?.Name;
                ViewBag.TitleValue = title;
                ViewBag.DescriptionValue = description;
                ViewBag.EventDateValue = eventDate;
                ViewBag.LocationValue = location;
                return View();
            }

            // Content Moderation Check
            if (!await _moderationService.IsContentSafeAsync(title) || 
                !await _moderationService.IsContentSafeAsync(description))
            {
                ViewBag.ErrorMessage = "🚫 localhost says: You need to be nice! 🤬\nConținutul tău conține termeni nepotriviți. Te rugăm să reformulezi.";
                ViewBag.GroupId = groupId;
                var group = await _groupService.GetGroupWithMembersAndMessagesAsync(groupId);
                ViewBag.GroupName = group?.Name;
                
                ViewBag.TitleValue = title;
                ViewBag.DescriptionValue = description;
                ViewBag.EventDateValue = eventDate;
                ViewBag.LocationValue = location;
                
                return View();
            }

            try
            {
                var eventId = await _groupService.CreateEventAsync(groupId, user.Id, title, description, eventDate, location);
                
                TempData["Success"] = "Eveniment creat cu succes!";
                return RedirectToAction("Details", new { id = eventId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", "Groups", new { id = groupId });
            }
        }

        // GET: Events/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var evt = await _groupService.GetEventDetailsAsync(id);
            if (evt == null) return NotFound();

            // Simple check: admin OR member OR moderator
            var isAdmin = User.IsInRole("Administrator");
            var isMember = evt.Group?.Members?.Any(m => m.UserId == user.Id && m.IsAccepted) ?? false;
            var isModerator = evt.Group?.ModeratorId == user.Id;
            
            if (!isAdmin && !isMember && !isModerator)
            {
                return Forbid();
            }

            ViewBag.CurrentUserId = user.Id;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.IsOrganizer = evt.OrganizerId == user.Id;
            ViewBag.IsModerator = isModerator;
            return View(evt);
        }

        // GET: Events/MyEvents
        [HttpGet]
        public async Task<IActionResult> MyEvents()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Get all groups user is part of
            var userGroups = await _groupService.GetUserGroupsAsync(user.Id);
            var allEvents = new List<Event>();

            // Get all events from all user's groups
            foreach (var group in userGroups)
            {
                var groupEvents = await _groupService.GetGroupEventsAsync(group.Id);
                allEvents.AddRange(groupEvents);
            }

            // Filter out past events - only show upcoming events
            var upcomingEvents = allEvents
                .Where(e => e.EventDate >= DateTime.Now)
                .OrderBy(e => e.EventDate)
                .ToList();

            ViewBag.CurrentUserId = user.Id;
            return View(upcomingEvents);
        }

        // POST: Events/RespondToEvent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondToEvent(int eventId, string status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!Enum.TryParse<EventParticipantStatus>(status, out var participationStatus))
            {
                return Json(new { success = false, message = "Status invalid." });
            }

            var success = await _groupService.RespondToEventAsync(eventId, user.Id, participationStatus);
            if (success)
            {
                return Json(new { success = true, message = "Răspuns înregistrat!" });
            }
            return Json(new { success = false, message = "Eroare la înregistrarea răspunsului." });
        }

        // POST: Events/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            try
            {
                var evt = await _groupService.GetEventDetailsAsync(id);
                if (evt == null) return NotFound();
                
                var groupId = evt.GroupId;
                var isAdmin = User.IsInRole("Administrator");
                
                // Admin can delete any event, otherwise check permissions
                if (!isAdmin && evt.OrganizerId != user.Id && evt.Group.ModeratorId != user.Id)
                {
                    return Forbid();
                }
                
                await _groupService.DeleteEventAsync(id, user.Id);
                
                TempData["Success"] = "Eveniment sters.";
                return RedirectToAction("Details", "Groups", new { id = groupId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", new { id });
            }
        }

        // GET: Events/GetGroupEvents?groupId=5 (AJAX endpoint)
        [HttpGet]
        public async Task<IActionResult> GetGroupEvents(int groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            try
            {
                var events = await _groupService.GetGroupEventsAsync(groupId);
                
                var result = events.Select(e => new
                {
                    id = e.Id,
                    title = e.Title,
                    description = e.Description,
                    eventDate = e.EventDate,
                    location = e.Location,
                    goingCount = e.Participants?.Count(p => p.Status == EventParticipantStatus.Going) ?? 0,
                    maybeCount = e.Participants?.Count(p => p.Status == EventParticipantStatus.Maybe) ?? 0,
                    notGoingCount = e.Participants?.Count(p => p.Status == EventParticipantStatus.NotGoing) ?? 0,
                    userStatus = e.Participants?.FirstOrDefault(p => p.UserId == user.Id)?.Status.ToString()
                }).ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // GET: Events/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var evt = await _groupService.GetEventDetailsAsync(id);
            if (evt == null) return NotFound();

            // Check permissions: organizer, group moderator, or admin
            var isAdmin = User.IsInRole("Administrator");
            var isOrganizer = evt.OrganizerId == user.Id;
            var isModerator = evt.Group?.ModeratorId == user.Id;

            if (!isAdmin && !isOrganizer && !isModerator)
            {
                return Forbid();
            }

            ViewBag.GroupId = evt.GroupId;
            ViewBag.GroupName = evt.Group?.Name;
            return View(evt);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string title, string description, DateTime eventDate, string? location)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var evt = await _groupService.GetEventDetailsAsync(id);
            if (evt == null) return NotFound();

            // Check permissions
            var isAdmin = User.IsInRole("Administrator");
            var isOrganizer = evt.OrganizerId == user.Id;
            var isModerator = evt.Group?.ModeratorId == user.Id;

            if (!isAdmin && !isOrganizer && !isModerator)
            {
                return Forbid();
            }

            // Content Moderation Check
            if (!await _moderationService.IsContentSafeAsync(title) || 
                !await _moderationService.IsContentSafeAsync(description))
            {
                ViewBag.ErrorMessage = "🚫 localhost says: You need to be nice! 🤬\nConținutul tău conține termeni nepotriviți. Te rugăm să reformulezi.";
                
                // Preserve user input
                evt.Title = title;
                evt.Description = description;
                evt.EventDate = eventDate;
                evt.Location = location;
                
                ViewBag.GroupId = evt.GroupId;
                ViewBag.GroupName = evt.Group?.Name;
                return View(evt);
            }

            try
            {
                // Update event
                evt.Title = title;
                evt.Description = description;
                evt.EventDate = eventDate;
                evt.Location = location;

                await _groupService.UpdateEventAsync(evt);
                
                TempData["Success"] = "Eveniment actualizat cu succes!";
                return RedirectToAction("Details", new { id = evt.Id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                
                // Preserve user input on error
                evt.Title = title;
                evt.Description = description;
                evt.EventDate = eventDate;
                evt.Location = location;
                
                ViewBag.GroupId = evt.GroupId;
                ViewBag.GroupName = evt.Group?.Name;
                return View(evt);
            }
        }
    }
}