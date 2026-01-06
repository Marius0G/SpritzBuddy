using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpritzBuddy.Data;
using SpritzBuddy.Models;
using SpritzBuddy.Models.ViewModels;
using SpritzBuddy.Services;
using System;
using System.Collections.Generic;
using System.Linq;
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
 private readonly ApplicationDbContext _context;
 private readonly ILogger<ProfileController> _logger;
 private readonly IGamificationService _gamificationService;

 public ProfileController(IProfileService profileService, UserManager<ApplicationUser> userManager, ApplicationDbContext context, ILogger<ProfileController> logger, IGamificationService gamificationService)
 {
 _profileService = profileService;
 _userManager = userManager;
 _context = context;
 _logger = logger;
 _gamificationService = gamificationService;
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
 DrinkStats = new List<DrinkStatViewModel>(),
 IsCurrentUser = false
 };

 return View(guestVm);
 }

 // Get real stats from database
 var postCount = await _context.Posts.CountAsync(p => p.UserId == targetUser.Id);
 var followersCount = await _context.Follows
 .CountAsync(f => f.FollowingId == targetUser.Id && f.Status == FollowStatus.Accepted);
 var followingCount = await _context.Follows
 .CountAsync(f => f.FollowerId == targetUser.Id && f.Status == FollowStatus.Accepted);

 // Get user posts for grid display
 var userPosts = await _context.Posts
 .Include(p => p.PostMedias)
 .Include(p => p.Likes)
 .Include(p => p.Comments)
 .Where(p => p.UserId == targetUser.Id)
 .OrderByDescending(p => p.CreateDate)
 .ToListAsync();

 // Build view model with data from the target user and real stats
 var vm = new ProfileViewModel
 {
 UserId = targetUser.Id,
 FirstName = targetUser.FirstName,
 LastName = targetUser.LastName,
 Description = targetUser.Description,
 ProfilePicturePath = targetUser.ProfilePictureUrl,
 IsPrivate = targetUser.IsPrivate,

 // real stats
 PostCount = postCount,
 FollowersCount = followersCount,
 FollowingCount = followingCount,

 // Get real gamification data
 Badges = await _gamificationService.GetUserBadgesAsync(targetUser.Id),
 DrinkStats = await _gamificationService.GetDrinkStatsAsync(targetUser.Id),

 // Posts for grid
 Posts = userPosts,

 IsCurrentUser = currentUser != null && currentUser.Id == targetUser.Id
 };

 // Check follow status if viewing another user's profile
 if (currentUser != null && currentUser.Id != targetUser.Id)
 {
 var followRecord = await _context.Follows
 .FirstOrDefaultAsync(f => f.FollowerId == currentUser.Id && f.FollowingId == targetUser.Id);

 if (followRecord != null)
 {
 if (followRecord.Status == FollowStatus.Accepted)
 {
 vm.IsFollowing = true;
 }
 else if (followRecord.Status == FollowStatus.Pending)
 {
 vm.HasPendingRequest = true;
 }
 }
 }

 return View(vm);
 }
 }
}
