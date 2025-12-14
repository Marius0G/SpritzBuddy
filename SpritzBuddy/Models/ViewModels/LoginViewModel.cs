using System.ComponentModel.DataAnnotations;

namespace SpritzBuddy.Models.ViewModels
{
 public class LoginViewModel
 {
 [Required]
 [Display(Name = "Email sau Username")]
 public string Identifier { get; set; } = string.Empty;

 [Required]
 [DataType(DataType.Password)]
 public string Password { get; set; } = string.Empty;

 [Display(Name = "?ine-m? minte")]
 public bool RememberMe { get; set; }
 }
}
