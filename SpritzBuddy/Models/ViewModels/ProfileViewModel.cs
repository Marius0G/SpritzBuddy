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

 // Drink statistics: drink name -> percentage
 public Dictionary<string, double>? DrinkStats { get; set; }

 // Whether the currently logged-in user is viewing their own profile
 public bool IsCurrentUser { get; set; }
 }
}
