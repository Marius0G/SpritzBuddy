using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SpritzBuddy.Models;
using System.Threading.Tasks;

namespace SpritzBuddy.Controllers
{
    [Authorize]
    public class DiagnosticController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public DiagnosticController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> CheckAdmin()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Content("User not authenticated");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var isAdmin = User.IsInRole("Administrator");
            var isAdminViaManager = await _userManager.IsInRoleAsync(user, "Administrator");

            var info = $@"
=== ADMIN DIAGNOSTIC ===

User Email: {user.Email}
User Username: {user.UserName}
User ID: {user.Id}

Roles: {string.Join(", ", roles)}
Count: {roles.Count}

User.IsInRole('Administrator'): {isAdmin}
UserManager.IsInRoleAsync('Administrator'): {isAdminViaManager}

=== END DIAGNOSTIC ===
            ";

            return Content(info, "text/plain");
        }
    }
}
