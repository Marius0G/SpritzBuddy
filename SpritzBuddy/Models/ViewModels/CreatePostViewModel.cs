using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SpritzBuddy.Models.ViewModels
{
    public class CreatePostViewModel
    {
        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;

        // Optional media files (up to 5 images)
        public List<IFormFile>? MediaFiles { get; set; }
    }
}
