using System.ComponentModel.DataAnnotations;

namespace SpritzBuddy.Models.ViewModels
{
    public class CreateGroupViewModel
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }
    }
}
