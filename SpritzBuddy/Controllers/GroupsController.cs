using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpritzBuddy.Models;
using SpritzBuddy.Models.ViewModels;
using SpritzBuddy.Services;

namespace SpritzBuddy.Controllers
{
    [Authorize]
    public class GroupsController : Controller
    {
        private readonly IGroupService _groupService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IContentModerationService _moderationService;

        public GroupsController(IGroupService groupService, UserManager<ApplicationUser> userManager, IContentModerationService moderationService)
        {
            _groupService = groupService;
            _userManager = userManager;
            _moderationService = moderationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Get only groups where user is a member (accepted) or moderator
            var myGroups = await _groupService.GetUserGroupsAsync(user.Id);
            return View(myGroups);
        }

        [HttpGet]
        public async Task<IActionResult> AllGroups()
        {
            // Get all groups for browsing/joining
            var allGroups = await _groupService.GetAllGroupsAsync();
            return View("Index", allGroups);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var currentUserId = user?.Id.ToString();

            var group = await _groupService.GetGroupWithMembersAndMessagesAsync(id);
            if (group == null)
                return NotFound();

            var isModerator = group.ModeratorId.ToString() == currentUserId;
            var isMember = group.Members?.Any(m => m.UserId.ToString() == currentUserId && m.IsAccepted) ?? false;
            var isPending = group.Members?.Any(m => m.UserId.ToString() == currentUserId && !m.IsAccepted) ?? false;
            var isAdmin = User.IsInRole("Administrator");

            if (!isAdmin && !isMember && !isModerator && !isPending)
            {
                // Note: We might want guests to see basic details, 
                // but let's stick to requirements or allow Admin bypass
            }

            var vm = new GroupDetailsViewModel
            {
                Group = group,
                CurrentUserId = currentUserId,
                IsModerator = isModerator || isAdmin, // Admin is treated as moderator for UI buttons
                IsMember = isMember || isAdmin,
                IsPending = isPending
            };
            return View(vm);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateGroupViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();
            
            if (!ModelState.IsValid)
                return View(model);

            // Content Moderation Check
            if (!await _moderationService.IsContentSafeAsync(model.Name) || 
                !await _moderationService.IsContentSafeAsync(model.Description))
            {
                ModelState.AddModelError("", "Conținutul tău conține termeni nepotriviți. Te rugăm să reformulezi.");
                ViewBag.ErrorMessage = "🚫 Content blocked: Your text contains inappropriate content (hate speech, harassment, or threats). Please rephrase respectfully.";
                return View(model);
            }

            var groupId = await _groupService.CreateGroupAsync(user.Id, model);
            TempData["SuccessMessage"] = "Grupul a fost creat cu succes!";
            return RedirectToAction("Details", new { id = groupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var result = await _groupService.JoinGroupAsync(user.Id, id);
            if (result)
                TempData["SuccessMessage"] = "Cererea a fost trimisă!";
            else
                TempData["ErrorMessage"] = "Ai trimis deja o cerere sau ești deja membru.";

            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var result = await _groupService.LeaveGroupAsync(user.Id, id);
            if (result)
            {
                TempData["SuccessMessage"] = "Ai părăsit grupul cu succes.";
            }
            else
            {
                TempData["ErrorMessage"] = "Nu poți părăsi acest grup (nu ești membru).";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int groupId, int userId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();
            try
            {
                await _groupService.AcceptMemberAsync(groupId, userId, user.Id);
                TempData["SuccessMessage"] = "Membrul a fost acceptat.";
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            return RedirectToAction("Details", new { id = groupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(int groupId, int userId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();
            try
            {
                await _groupService.RemoveMemberAsync(groupId, userId, user.Id);
                TempData["SuccessMessage"] = "Cererea a fost respinsă.";
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            return RedirectToAction("Details", new { id = groupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Kick(int groupId, int userId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();
            try
            {
                await _groupService.RemoveMemberAsync(groupId, userId, user.Id);
                TempData["SuccessMessage"] = "Membrul a fost eliminat.";
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            return RedirectToAction("Details", new { id = groupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();
            try
            {
                if (User.IsInRole("Administrator"))
                {
                    await _groupService.DeleteGroupAsAdminAsync(groupId);
                }
                else
                {
                    await _groupService.DeleteGroupAsync(groupId, user.Id);
                }
                
                TempData["SuccessMessage"] = "Grupul a fost șters.";
                return RedirectToAction("Index");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostMessage(int groupId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();
            
            // Content Moderation Check
            if (!await _moderationService.IsContentSafeAsync(content))
            {
                TempData["ErrorMessage"] = "Conținutul tău conține termeni nepotriviți. Te rugăm să reformulezi.";
                TempData["DraftMessage"] = content; // Preserve content
                return RedirectToAction("Details", new { id = groupId });
            }
            
            try
            {
                await _groupService.PostMessageAsync(groupId, user.Id, content);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            return RedirectToAction("Details", new { id = groupId });
        }

        [HttpGet]
        public async Task<IActionResult> EditMessage(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();
            
            var isAdmin = User.IsInRole("Administrator");
            var message = await _groupService.GetMessageForEditAsync(id, user.Id);
            
            // If not found and user is admin, try to get message without user check
            if (message == null && isAdmin)
            {
                message = await _groupService.GetMessageByIdAsync(id);
            }
            
            if (message == null)
                return Forbid();
                
            return View(message);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMessage(int id, string newContent)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();
            
            // Get the message first to preserve it if moderation fails
            var message = await _groupService.GetMessageForEditAsync(id, user.Id);
            var isAdmin = User.IsInRole("Administrator");
            
            if (message == null && isAdmin)
            {
                message = await _groupService.GetMessageByIdAsync(id);
            }
            
            if (message == null)
                return Forbid();
            
            // Content Moderation Check
            if (!await _moderationService.IsContentSafeAsync(newContent))
            {
                TempData["ErrorMessage"] = "Conținutul tău conține termeni nepotriviți. Te rugăm să reformulezi.";
                
                // Preserve user input
                message.Content = newContent;
                return View(message);
            }
            
            try
            {
                if (isAdmin)
                {
                    await _groupService.EditMessageAsAdminAsync(id, newContent);
                }
                else
                {
                    await _groupService.EditMessageAsync(id, user.Id, newContent);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            
            var groupIdFinal = await _groupService.GetGroupIdForMessageAsync(id);
            if (groupIdFinal.HasValue)
                return RedirectToAction("Details", new { id = groupIdFinal.Value });
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();
            
            var isAdmin = User.IsInRole("Administrator");
            int? groupId = null;
            
            try
            {
                groupId = await _groupService.GetGroupIdForMessageAsync(id);
                
                if (isAdmin)
                {
                    await _groupService.DeleteMessageAsAdminAsync(id);
                }
                else
                {
                    await _groupService.DeleteMessageAsync(id, user.Id);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            
            if (groupId.HasValue)
                return RedirectToAction("Details", new { id = groupId.Value });
            return RedirectToAction("Index");
        }

        // Invite functionality
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInvite(int groupId, int userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var success = await _groupService.SendInviteAsync(groupId, currentUser.Id, userId);
            if (success)
                TempData["Success"] = "Invitația a fost trimisă cu succes.";
            else
                TempData["Error"] = "Nu s-a putut trimite invitația.";

            return RedirectToAction("Details", new { id = groupId });
        }

        [HttpGet]
        public async Task<IActionResult> MyInvites()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var invites = await _groupService.GetPendingInvitesForUserAsync(user.Id);
            return View(invites);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptInvite(int inviteId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            try
            {
                await _groupService.AcceptInviteAsync(inviteId, user.Id);
                TempData["Success"] = "Ai acceptat invitația cu succes.";
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }

            return RedirectToAction("Index", "Notifications");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineInvite(int inviteId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            try
            {
                await _groupService.DeclineInviteAsync(inviteId, user.Id);
                TempData["Success"] = "Ai refuzat invitația.";
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }

            return RedirectToAction("Index", "Notifications");
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingInvitesCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { count = 0 });

            var invites = await _groupService.GetPendingInvitesForUserAsync(user.Id);
            return Json(new { count = invites.Count });
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string query, int groupId)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new List<object>());
            }

            var searchTerm = query.Trim().ToLower();
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            // Get group to verify moderator
            var group = await _groupService.GetGroupWithMembersAndMessagesAsync(groupId);
            if (group == null || group.ModeratorId != currentUser.Id)
                return Forbid();

            // Get current members and pending invites
            var memberIds = group.Members?.Select(m => m.UserId).ToList() ?? new List<int>();
            var pendingInviteUserIds = await _groupService.GetPendingInvitesForGroupAsync(groupId);

            // Search users excluding current members and those with pending invites
            var users = await _userManager.Users
                .Where(u => 
                    (u.UserName != null && u.UserName.ToLower().Contains(searchTerm) ||
                     u.FirstName.ToLower().Contains(searchTerm) ||
                     u.LastName.ToLower().Contains(searchTerm)) &&
                    !memberIds.Contains(u.Id) &&
                    !pendingInviteUserIds.Contains(u.Id) &&
                    u.Id != currentUser.Id)
                .Take(10)
                .Select(u => new 
                { 
                    id = u.Id,
                    username = u.UserName,
                    fullName = u.FirstName + " " + u.LastName,
                    profilePicture = u.ProfilePictureUrl
                })
                .ToListAsync();

            return Json(users);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var group = await _groupService.GetGroupWithMembersAndMessagesAsync(id);
            if (group == null)
                return NotFound();

            // Only moderator or admin can edit
            if (group.ModeratorId != user.Id && !User.IsInRole("Administrator"))
                return Forbid();

            return View(group);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name, string description)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var group = await _groupService.GetGroupWithMembersAndMessagesAsync(id);
            if (group == null)
                return NotFound();

            // Only moderator or admin can edit
            if (group.ModeratorId != user.Id && !User.IsInRole("Administrator"))
                return Forbid();

            // Content Moderation Check
            if (!await _moderationService.IsContentSafeAsync(name) || 
                !await _moderationService.IsContentSafeAsync(description))
            {
                ViewBag.ErrorMessage = "🚫 Content blocked: Your text contains inappropriate content (hate speech, harassment, or threats). Please rephrase respectfully.";
                
                // Preserve user input
                group.Name = name;
                group.Description = description;
                return View(group);
            }

            try
            {
                await _groupService.UpdateGroupAsync(id, name, description);
                TempData["SuccessMessage"] = "Grupul a fost actualizat cu succes!";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                
                // Preserve user input
                group.Name = name;
                group.Description = description;
                return View(group);
            }
        }
    }
}
