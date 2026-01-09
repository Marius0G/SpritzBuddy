using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SpritzBuddy.Models;
using SpritzBuddy.Services;

namespace SpritzBuddy.Controllers
{
    [Authorize]
    public class EventsController : Controller
    {
        private readonly IGroupService _groupService;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventsController(IGroupService groupService, UserManager<ApplicationUser> userManager)
        {
            _groupService = groupService;
            _userManager = userManager;
        }

        // GET: Events/Create?groupId=1
        [HttpGet]
        public async Task<IActionResult> Create(int groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Verify user is a member of the group
            var group = await _groupService.GetGroupWithMembersAndMessagesAsync(groupId);
            if (group == null)
                return NotFound();

            var isMember = group.Members?.Any(m => m.UserId == user.Id && m.IsAccepted) ?? false;
            if (!isMember && group.ModeratorId != user.Id)
            {
                TempData["Error"] = "Trebuie s? fii membru al grupului pentru a crea evenimente.";
                return RedirectToAction("Details", "Groups", new { id = groupId });
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
            if (user == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
            {
                TempData["Error"] = "Titlul ?i descrierea sunt obligatorii.";
                return RedirectToAction("Create", new { groupId });
            }

            if (eventDate <= DateTime.Now)
            {
                TempData["Error"] = "Data evenimentului trebuie s? fie în viitor.";
                return RedirectToAction("Create", new { groupId });
            }

            try
            {
                var eventId = await _groupService.CreateEventAsync(groupId, user.Id, title, description, eventDate, location);
                TempData["Success"] = "Evenimentul a fost creat cu succes!";
                return RedirectToAction("Details", new { id = eventId });
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Nu ai permisiunea s? creezi evenimente în acest grup.";
                return RedirectToAction("Details", "Groups", new { id = groupId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Eroare la crearea evenimentului: {ex.Message}";
                return RedirectToAction("Create", new { groupId });
            }
        }

        // GET: Events/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var groupEvent = await _groupService.GetEventDetailsAsync(id);
            if (groupEvent == null)
                return NotFound();

            // Verify user is a member of the group
            var isMember = groupEvent.Group.Members?.Any(m => m.UserId == user.Id && m.IsAccepted) ?? false;
            if (!isMember && groupEvent.Group.ModeratorId != user.Id)
            {
                TempData["Error"] = "Trebuie s? fii membru al grupului pentru a vedea acest eveniment.";
                return RedirectToAction("Index", "Groups");
            }

            ViewBag.CurrentUserId = user.Id;
            return View(groupEvent);
        }

        // POST: Events/Respond
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(int eventId, string status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "Utilizator neautentificat." });

            if (!Enum.TryParse<EventParticipationStatus>(status, out var participationStatus))
            {
                return Json(new { success = false, message = "Status invalid." });
            }

            var success = await _groupService.RespondToEventAsync(eventId, user.Id, participationStatus);
            if (success)
            {
                var statusText = participationStatus switch
                {
                    EventParticipationStatus.Going => "Vin",
                    EventParticipationStatus.Maybe => "Poate",
                    EventParticipationStatus.NotGoing => "Nu vin",
                    _ => ""
                };
                return Json(new { success = true, message = $"R?spunsul t?u ({statusText}) a fost înregistrat!" });
            }
            else
            {
                return Json(new { success = false, message = "Nu s-a putut înregistra r?spunsul." });
            }
        }

        // GET: Events/List?groupId=1
        [HttpGet]
        public async Task<IActionResult> List(int groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var group = await _groupService.GetGroupWithMembersAndMessagesAsync(groupId);
            if (group == null)
                return NotFound();

            var isMember = group.Members?.Any(m => m.UserId == user.Id && m.IsAccepted) ?? false;
            if (!isMember && group.ModeratorId != user.Id)
            {
                TempData["Error"] = "Trebuie s? fii membru al grupului pentru a vedea evenimentele.";
                return RedirectToAction("Index", "Groups");
            }

            var events = await _groupService.GetGroupEventsAsync(groupId);
            ViewBag.GroupId = groupId;
            ViewBag.GroupName = group.Name;
            ViewBag.CurrentUserId = user.Id;
            return View(events);
        }

        // GET: Events/GetJson?groupId=1 (for AJAX calls)
        [HttpGet]
        public async Task<IActionResult> GetJson(int groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "Utilizator neautentificat." });

            // Verify user is a member of the group
            var group = await _groupService.GetGroupWithMembersAndMessagesAsync(groupId);
            if (group == null)
                return Json(new { success = false, message = "Grupul nu exist?." });

            var isMember = group.Members?.Any(m => m.UserId == user.Id && m.IsAccepted) ?? false;
            if (!isMember && group.ModeratorId != user.Id)
                return Json(new { success = false, message = "Nu ai acces la acest grup." });

            var events = await _groupService.GetGroupEventsAsync(groupId);
            
            var eventDtos = events.Select(e => new
            {
                id = e.Id,
                title = e.Title,
                description = e.Description,
                eventDate = e.EventDate,
                location = e.Location,
                creatorName = e.Creator?.UserName,
                goingCount = e.Participants?.Count(p => p.Status == EventParticipationStatus.Going) ?? 0,
                maybeCount = e.Participants?.Count(p => p.Status == EventParticipationStatus.Maybe) ?? 0,
                notGoingCount = e.Participants?.Count(p => p.Status == EventParticipationStatus.NotGoing) ?? 0,
                userStatus = e.Participants?.FirstOrDefault(p => p.UserId == user.Id)?.Status.ToString()
            }).ToList();

            return Json(eventDtos);
        }

        // POST: Events/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            try
            {
                var groupEvent = await _groupService.GetEventDetailsAsync(id);
                var groupId = groupEvent?.GroupId;
                
                await _groupService.DeleteEventAsync(id, user.Id);
                TempData["Success"] = "Evenimentul a fost ?ters cu succes.";
                
                if (groupId.HasValue)
                    return RedirectToAction("Details", "Groups", new { id = groupId.Value });
                
                return RedirectToAction("Index", "Groups");
            }
            catch (UnauthorizedAccessException)
            {
                TempData["Error"] = "Nu ai permisiunea s? ?tergi acest eveniment. Doar creatorul sau moderatorul grupului pot ?terge evenimente.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Eroare la ?tergerea evenimentului: {ex.Message}";
                return RedirectToAction("Details", new { id });
            }
        }

        // GET: Events/MyEvents (All events user is involved in)
        [HttpGet]
        public async Task<IActionResult> MyEvents()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Get all groups user is a member of
            var userGroupIds = await _groupService.GetUserGroupsAsync(user.Id);
            var groupIds = userGroupIds.Select(g => g.Id).ToList();

            // Get all events from those groups
            var allEvents = new List<GroupEvent>();
            foreach (var groupId in groupIds)
            {
                var events = await _groupService.GetGroupEventsAsync(groupId);
                allEvents.AddRange(events);
            }

            // Sort by date
            var sortedEvents = allEvents
                .OrderBy(e => e.EventDate)
                .ToList();

            ViewBag.CurrentUserId = user.Id;
            return View(sortedEvents);
        }
    }
}
