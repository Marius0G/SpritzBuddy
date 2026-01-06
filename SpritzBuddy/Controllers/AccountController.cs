using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SpritzBuddy.Models;
using SpritzBuddy.Models.ViewModels;
using System.Threading.Tasks;

namespace SpritzBuddy.Controllers
{
 public class AccountController : Controller
 {
 private readonly UserManager<ApplicationUser> _userManager;
 private readonly SignInManager<ApplicationUser> _signInManager;

 public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
 {
 _userManager = userManager;
 _signInManager = signInManager;
 }

 [HttpGet]
 public IActionResult Register()
 {
 return View();
 }

 [HttpPost]
 [ValidateAntiForgeryToken]
 public async Task<IActionResult> Register(RegisterViewModel model)
 {
 if (!ModelState.IsValid)
 return View(model);

 var user = new ApplicationUser
 {
 UserName = model.Username,
 Email = model.Email,
 FirstName = model.FirstName,
 LastName = model.LastName,
 CreatedDate = DateTime.UtcNow,
 LastActiveDate = DateTime.UtcNow
 };

 var result = await _userManager.CreateAsync(user, model.Password);
 if (result.Succeeded)
 {
 // assign "User" role
 await _userManager.AddToRoleAsync(user, "User");

 await _signInManager.SignInAsync(user, isPersistent: false);
 return RedirectToAction("Index", "Home");
 }

 foreach (var err in result.Errors)
 {
 ModelState.AddModelError(string.Empty, err.Description);
 }

 return View(model);
 }

 [HttpGet]
 public IActionResult Login()
 {
 return View();
 }

 [HttpPost]
 [ValidateAntiForgeryToken]
 public async Task<IActionResult> Login(LoginViewModel model)
 {
 if (!ModelState.IsValid)
 return View(model);

 // Allow login with either email or username.
 var identifier = model.Identifier?.Trim() ?? string.Empty;

 string? userNameToSignIn = identifier;

 if (!string.IsNullOrEmpty(identifier))
 {
 // If input looks like an email, try to resolve to user by email first
 if (identifier.Contains("@"))
 {
 var byEmail = await _userManager.FindByEmailAsync(identifier);
 if (byEmail != null)
 {
 userNameToSignIn = byEmail.UserName;
 }
 }
 else
 {
 // try to find by username; if found, use canonical username
 var byName = await _userManager.FindByNameAsync(identifier);
 if (byName != null)
 {
 userNameToSignIn = byName.UserName;
 }
 }
 }

 if (string.IsNullOrEmpty(userNameToSignIn))
 {
 ModelState.AddModelError(string.Empty, "Invalid login attempt.");
 return View(model);
 }

 var result = await _signInManager.PasswordSignInAsync(userNameToSignIn, model.Password, model.RememberMe, lockoutOnFailure: false);
 if (result.Succeeded)
 {
 return RedirectToAction("Index", "Home");
 }

 ModelState.AddModelError(string.Empty, "Invalid login attempt.");
 return View(model);
 }

 [HttpPost]
 [ValidateAntiForgeryToken]
 public async Task<IActionResult> Logout()
 {
 await _signInManager.SignOutAsync();
 return RedirectToAction("Index", "Home");
 }

 [HttpGet]
 public IActionResult AccessDenied(string returnUrl = "")
 {
  ViewData["ReturnUrl"] = returnUrl;
  return View();
 }
 }
}
