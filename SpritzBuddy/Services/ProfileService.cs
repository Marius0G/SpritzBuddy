using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SpritzBuddy.Data;
using SpritzBuddy.Models;
using SpritzBuddy.Models.ViewModels;

namespace SpritzBuddy.Services
{
 public class ProfileService : IProfileService
 {
 private readonly ApplicationDbContext _dbContext;
 private readonly IFileUploadService _fileUploadService;
 private readonly UserManager<ApplicationUser> _userManager;
 private readonly ILogger<ProfileService> _logger;

 public ProfileService(
 ApplicationDbContext dbContext,
 IFileUploadService fileUploadService,
 UserManager<ApplicationUser> userManager,
 ILogger<ProfileService> logger)
 {
 _dbContext = dbContext;
 _fileUploadService = fileUploadService;
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

 // Handle profile image upload using the file upload service
 if (model.ProfileImage != null && model.ProfileImage.Length > 0)
 {
 _logger.LogInformation("Profile image present (length={Length}) for user {UserId}", model.ProfileImage.Length, userId);
 
 try
 {
 // Delete old profile picture if exists
 if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
 {
 await _fileUploadService.DeleteProfilePictureAsync(user.ProfilePictureUrl);
 }

 // Upload new profile picture
 var newProfileUrl = await _fileUploadService.UploadProfilePictureAsync(model.ProfileImage, intId);
 user.ProfilePictureUrl = newProfileUrl;
 
 _logger.LogInformation("Profile picture uploaded successfully for user {UserId}", userId);
 }
 catch (ArgumentException ex)
 {
 _logger.LogWarning(ex, "Invalid file upload for user {UserId}", userId);
 return false;
 }
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
