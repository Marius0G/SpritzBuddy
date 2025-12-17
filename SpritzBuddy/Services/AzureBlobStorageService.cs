using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace SpritzBuddy.Services
{
    public class AzureBlobStorageService : IFileUploadService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "profile-pictures";
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

        public AzureBlobStorageService(string connectionString)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadProfilePictureAsync(IFormFile file, int userId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            if (!IsValidImageFile(file))
                throw new ArgumentException("Invalid file type or size");

            // Get or create container
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // Create unique blob name
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var blobName = $"profile_{userId}_{Guid.NewGuid()}{fileExtension}";

            // Get blob client
            var blobClient = containerClient.GetBlobClient(blobName);

            // Set content type
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = file.ContentType
            };

            // Upload file
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders
                });
            }

            // Return public URL
            return blobClient.Uri.ToString();
        }

        public async Task<bool> DeleteProfilePictureAsync(string blobUrl)
        {
            if (string.IsNullOrEmpty(blobUrl))
                return false;

            try
            {
                // Extract blob name from URL
                var uri = new Uri(blobUrl);
                var blobName = Path.GetFileName(uri.LocalPath);

                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                return await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool IsValidImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > _maxFileSize)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                return false;

            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                return false;

            return true;
        }
    }
}
