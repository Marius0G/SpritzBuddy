using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SpritzBuddy.Models.ViewModels
{
 public class EditProfileViewModel
 {
 [Required]
 [MaxLength(100)]
 public string FirstName { get; set; } = string.Empty;

 [Required]
 [MaxLength(100)]
 public string LastName { get; set; } = string.Empty;

 [MaxLength(250)]
 public string? Description { get; set; }

 public bool IsPrivate { get; set; }

 // Optional uploaded profile image
 public IFormFile? ProfileImage { get; set; }
 }
}
