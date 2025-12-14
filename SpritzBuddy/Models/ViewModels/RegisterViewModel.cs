using System.ComponentModel.DataAnnotations;

namespace SpritzBuddy.Models.ViewModels
{
 public class RegisterViewModel
 {
 [Required]
 [StringLength(100, MinimumLength =3)]
 [Display(Name = "Username")]
 public string Username { get; set; } = string.Empty;

 [Required]
 [EmailAddress]
 [Display(Name = "Email")]
 public string Email { get; set; } = string.Empty;

 [Required]
 [DataType(DataType.Password)]
 [StringLength(100, MinimumLength =6, ErrorMessage = "The {0} must be at least {2} characters long.")]
 public string Password { get; set; } = string.Empty;

 [Required]
 [DataType(DataType.Password)]
 [Display(Name = "Confirm password")]
 [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
 public string ConfirmPassword { get; set; } = string.Empty;

 [Required]
 [StringLength(100)]
 [Display(Name = "First Name")]
 public string FirstName { get; set; } = string.Empty;

 [Required]
 [StringLength(100)]
 [Display(Name = "Last Name")]
 public string LastName { get; set; } = string.Empty;
 }
}
