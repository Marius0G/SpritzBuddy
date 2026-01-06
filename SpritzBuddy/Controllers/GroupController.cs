using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        public GroupsController(IGroupService groupService, UserManager<ApplicationUser> userManager)
        {
            _groupService = groupService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var groups = await _groupService.GetAllGroupsAsync();
            return View("~/Views/Group/Index.cshtml", groups);
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

            var vm = new GroupDetailsViewModel
            {
                Group = group,
                CurrentUserId = currentUserId,
                IsModerator = isModerator,
                IsMember = isMember,
                IsPending = isPending
            };
            return View("~/Views/Group/Details.cshtml", vm);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View("~/Views/Group/Create.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateGroupViewModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Group/Create.cshtml", model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var groupId = await _groupService.CreateGroupAsync(user.Id, model);
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
                await _groupService.DeleteGroupAsync(groupId, user.Id);
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
            var message = await _groupService.GetMessageForEditAsync(id, user.Id);
            if (message == null)
                return Forbid();
            return View("~/Views/Group/EditMessage.cshtml", message);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMessage(int id, string newContent)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();
            try
            {
                await _groupService.EditMessageAsync(id, user.Id, newContent);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            var groupId = await _groupService.GetGroupIdForMessageAsync(id);
            if (groupId.HasValue)
                return RedirectToAction("Details", new { id = groupId.Value });
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();
            int? groupId = null;
            try
            {
                groupId = await _groupService.GetGroupIdForMessageAsync(id);
                await _groupService.DeleteMessageAsync(id, user.Id);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            if (groupId.HasValue)
                return RedirectToAction("Details", new { id = groupId.Value });
            return RedirectToAction("Index");
        }
    }
}
