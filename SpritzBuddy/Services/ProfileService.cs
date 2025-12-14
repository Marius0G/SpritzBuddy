using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpritzBuddy.Data;
using SpritzBuddy.Models;
using SpritzBuddy.Models.ViewModels;
using System.IO;

namespace SpritzBuddy.Services
{
 public class ProfileService : IProfileService
 {
 private readonly ApplicationDbContext _dbContext;
 private readonly IWebHostEnvironment _env;
 private readonly UserManager<ApplicationUser> _userManager;
 private readonly ILogger<ProfileService> _logger;

 public ProfileService(ApplicationDbContext dbContext, IWebHostEnvironment env, UserManager<ApplicationUser> userManager, ILogger<ProfileService> logger)
 {
 _dbContext = dbContext;
 _env = env;
 _userManager = userManager;
 _logger = logger;
 }

 public async Task<bool> UpdateProfileAsync(string userId, EditProfileViewModel model)
 {
 _logger.LogInformation("UpdateProfileAsync called for userId={UserId}", userId);
 try
 {
 if (string.IsNullOrWhiteSpace(userId) || model == null)
 return false;

 if (!int.TryParse(userId, out var intId))
 return false;

 var user = await _dbContext.ApplicationUsers
 .FirstOrDefaultAsync(u => u.Id == intId);

 if (user == null)
 return false;

 _logger.LogInformation("Loaded user {UserId} from db", intId);

 user.FirstName = model.FirstName;
 user.LastName = model.LastName;
 user.Description = model.Description;
 user.IsPrivate = model.IsPrivate;
 user.LastActiveDate = DateTime.UtcNow;

 // Handle profile image upload
 if (model.ProfileImage != null && model.ProfileImage.Length >0)
 {
 _logger.LogInformation("Profile image present (length={Length}) for user {UserId}", model.ProfileImage.Length, userId);
 // Basic validation
 var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
 var ext = Path.GetExtension(model.ProfileImage.FileName)?.ToLowerInvariant() ?? string.Empty;
 if (!allowed.Contains(ext))
 {
 _logger.LogWarning("Rejected upload with extension {Ext} for user {UserId}", ext, userId);
 return false;
 }

 const long maxBytes =5 *1024 *1024; //5 MB
 if (model.ProfileImage.Length > maxBytes)
 {
 _logger.LogWarning("Rejected upload too large ({Length} bytes) for user {UserId}", model.ProfileImage.Length, userId);
 return false;
 }

 var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "profiles");
 _logger.LogInformation("Uploads root resolved to {Path}", uploadsRoot);
 if (!Directory.Exists(uploadsRoot))
 {
 _logger.LogInformation("Creating uploads directory {Path}", uploadsRoot);
 Directory.CreateDirectory(uploadsRoot);
 }

 var fileName = $"{Guid.NewGuid()}{ext}";
 var filePath = Path.Combine(uploadsRoot, fileName);
 _logger.LogInformation("Saving uploaded file to {FilePath}", filePath);

 await using (var stream = new FileStream(filePath, FileMode.Create))
 {
 await model.ProfileImage.CopyToAsync(stream);
 }
 _logger.LogInformation("File saved to disk for user {UserId}", userId);

 // store relative URL
 user.ProfilePictureUrl = $"/uploads/profiles/{fileName}";
 }

 _logger.LogInformation("Saving changes to database for user {UserId}", userId);
 _dbContext.ApplicationUsers.Update(user);
 await _dbContext.SaveChangesAsync();
 _logger.LogInformation("Database save completed for user {UserId}", userId);

 return true;
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error updating profile for userId={UserId}", userId);
 return false;
 }
 }
 }
}
