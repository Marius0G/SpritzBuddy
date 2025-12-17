namespace SpritzBuddy.Services
{
    public interface IFileUploadService
    {
        Task<string> UploadProfilePictureAsync(IFormFile file, int userId);
        Task<bool> DeleteProfilePictureAsync(string filePath);
        bool IsValidImageFile(IFormFile file);
    }
}
