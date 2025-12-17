using Microsoft.AspNetCore.Http;

namespace SpritzBuddy.Services
{
    public interface IPostMediaService
    {
        Task<List<string>> UploadPostMediaAsync(IEnumerable<IFormFile> files, int postId);
        Task<bool> DeletePostMediaAsync(string filePath);
        bool IsValidImageFile(IFormFile file);
    }
}
