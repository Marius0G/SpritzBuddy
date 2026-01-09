using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SpritzBuddy.Services
{
    public class PostMediaService : IPostMediaService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<PostMediaService> _logger;
        private readonly long _maxFileSize = 50 * 1024 * 1024; // 50MB per file (increased for video)
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".mov", ".avi", ".webm" };

        public PostMediaService(IWebHostEnvironment environment, ILogger<PostMediaService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<List<string>> UploadPostMediaAsync(IEnumerable<IFormFile> files, int postId)
        {
            var uploadedPaths = new List<string>();

            if (files == null || !files.Any())
            {
                _logger.LogWarning("No files provided for upload");
                return uploadedPaths;
            }

            // Create directory if it doesn't exist
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "posts");
            
            try
            {
                Directory.CreateDirectory(uploadsFolder);
                _logger.LogInformation($"Upload directory ensured: {uploadsFolder}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create directory: {uploadsFolder}");
                throw;
            }

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("Skipping null or empty file");
                    continue;
                }

                if (!IsValidImageFile(file))
                {
                    _logger.LogWarning($"File {file.FileName} failed validation");
                    continue;
                }

                // Create unique filename
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileName = $"post_{postId}_{Guid.NewGuid()}{fileExtension}";

                // Full file path
                var filePath = Path.Combine(uploadsFolder, fileName);

                try
                {
                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Add relative URL path to list
                    var relativePath = $"/uploads/posts/{fileName}";
                    uploadedPaths.Add(relativePath);
                    
                    _logger.LogInformation($"Successfully uploaded file: {fileName} -> {relativePath}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to upload file {fileName}");
                }
            }

            _logger.LogInformation($"Total files uploaded: {uploadedPaths.Count}");
            return uploadedPaths;
        }

        public async Task<bool> DeletePostMediaAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogWarning("Attempted to delete with null/empty file path");
                return false;
            }

            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));

                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));
                    _logger.LogInformation($"Deleted file: {fullPath}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"File not found for deletion: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file: {filePath}");
                return false;
            }

            return false;
        }

        public bool IsValidImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("File is null or empty");
                return false;
            }

            // Check file size
            if (file.Length > _maxFileSize)
            {
                _logger.LogWarning($"File {file.FileName} exceeds max size: {file.Length} bytes");
                return false;
            }

            // Check extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                _logger.LogWarning($"File {file.FileName} has invalid extension: {extension}");
                return false;
            }

            // Check MIME type
            var allowedMimeTypes = new[] { 
                "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp",
                "video/mp4", "video/quicktime", "video/x-msvideo", "video/webm"
            };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                _logger.LogWarning($"File {file.FileName} has invalid MIME type: {file.ContentType}");
                return false;
            }

            _logger.LogInformation($"File {file.FileName} passed validation");
            return true;
        }
    }
}
