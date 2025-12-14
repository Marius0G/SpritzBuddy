using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpritzBuddy.Models;
using SpritzBuddy.Models.ViewModels;
using SpritzBuddy.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpritzBuddy.Controllers
{
 // Use attribute routing so /Profile/{username} works
 [Route("Profile")]
 [Authorize]
 public class ProfileController : Controller
 {
 private readonly IProfileService _profileService;
 private readonly UserManager<ApplicationUser> _userManager;
 private readonly ILogger<ProfileController> _logger;

 public ProfileController(IProfileService profileService, UserManager<ApplicationUser> userManager, ILogger<ProfileController> logger)
 {
 _profileService = profileService;
 _userManager = userManager;
 _logger = logger;
 }

 [HttpGet("Edit")]
 public async Task<IActionResult> Edit()
 {
 var user = await _userManager.GetUserAsync(User);
 if (user == null)
 return Challenge();

 var vm = new EditProfileViewModel
 {
 FirstName = user.FirstName,
 LastName = user.LastName,
 Description = user.Description,
 IsPrivate = user.IsPrivate
 };

 // expose current picture to the view
 ViewBag.ProfilePictureUrl = user.ProfilePictureUrl;

 return View(vm);
 }

 [HttpPost("Edit")]
 [ValidateAntiForgeryToken]
 public async Task<IActionResult> Edit(EditProfileViewModel model)
 {
 if (!ModelState.IsValid)
 return View(model);

 var user = await _userManager.GetUserAsync(User);
 if (user == null)
 return Challenge();

 try
 {
 var updated = await _profileService.UpdateProfileAsync(user.Id.ToString(), model);
 if (!updated)
 {
 ModelState.AddModelError(string.Empty, "Failed to update profile.");
 return View(model);
 }

 return RedirectToAction("Index", "Profile");
 }
 catch (System.Exception ex)
 {
 _logger.LogError(ex, "Unhandled error updating profile for user {UserId}", user.Id);
 ModelState.AddModelError(string.Empty, "Unexpected error while updating profile. Please try again later.");
 return View(model);
 }
 }

 // Now accepts username in the route: /Profile or /Profile/{username}
 [HttpGet("{username?}")]
 [AllowAnonymous]
 public async Task<IActionResult> Index(string? username)
 {
 // Get currently authenticated user (may be null)
 var currentUser = await _userManager.GetUserAsync(User);

 ApplicationUser? targetUser = null;

 if (!string.IsNullOrWhiteSpace(username))
 {
 // look up the requested user by username
 targetUser = await _userManager.FindByNameAsync(username);
 if (targetUser == null)
 {
 return NotFound();
 }
 }
 else
 {
 // if no username provided, show current user's profile
 targetUser = currentUser;
 }

 // If still no user (not logged in and no username), show guest view
 if (targetUser == null)
 {
 var guestVm = new ProfileViewModel
 {
 FirstName = "Guest",
 LastName = string.Empty,
 Description = string.Empty,
 ProfilePicturePath = null,
 PostCount =0,
 FollowersCount =0,
 FollowingCount =0,
 Badges = new List<string> { "Visitor" },
 DrinkStats = new Dictionary<string, double> { { "Aperol",60 }, { "Bere",40 } },
 IsCurrentUser = false
 };

 return View(guestVm);
 }

 // Build view model with data from the target user and mock stats
 var vm = new ProfileViewModel
 {
 FirstName = targetUser.FirstName,
 LastName = targetUser.LastName,
 Description = targetUser.Description,
 ProfilePicturePath = targetUser.ProfilePictureUrl,

 // mock stats
 PostCount =3,
 FollowersCount =42,
 FollowingCount =10,

 Badges = new List<string> { "Newbie", "Night Owl" },
 DrinkStats = new Dictionary<string, double> { { "Aperol",60 }, { "Bere",40 } },

 IsCurrentUser = currentUser != null && currentUser.Id == targetUser.Id
 };

 return View(vm);
 }
 }
}
