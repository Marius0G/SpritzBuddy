using System.Collections.Generic;

namespace SpritzBuddy.Models.ViewModels
{
 public class ProfileViewModel
 {
 // User info
 public string FirstName { get; set; } = string.Empty;
 public string LastName { get; set; } = string.Empty;
 public string? Description { get; set; }
 public string? ProfilePicturePath { get; set; }

 // Stats placeholders
 public int PostCount { get; set; }
 public int FollowersCount { get; set; }
 public int FollowingCount { get; set; }

 // Gamification placeholders
 public List<string>? Badges { get; set; }

 // Drink statistics with colors
 public List<DrinkStatViewModel>? DrinkStats { get; set; }

 // User posts for grid display
 public List<Post>? Posts { get; set; }

 // Whether the currently logged-in user is viewing their own profile
 public bool IsCurrentUser { get; set; }

 // Follow status
 public int UserId { get; set; }
 public bool IsFollowing { get; set; }
 public bool HasPendingRequest { get; set; }
 public bool IsPrivate { get; set; }
 }
}
