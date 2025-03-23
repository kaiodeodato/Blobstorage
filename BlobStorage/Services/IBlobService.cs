using Azure.Storage.Blobs.Models;
using BlobStorage.Models;

namespace BlobStorage.Services
{
    public interface IBlobService
    {
        Task<IEnumerable<BlobItemWithMetadata>> ListFilesAsync();
        Task<bool> UploadFileAsync(IFormFile file, string description);
        Task<BlobDownloadInfo?> DownloadFileAsync(string fileName);
        Task<bool> DeleteFileAsync(string fileName);
        Task<bool> EditBlobDescriptionAsync(string fileName, string newDescription);
        Task<string> GenerateThumbnailAsync(string fileName);
        Task<string> ShowImageAsync(string fileName);
        Task<Models.OperationResult> RenameBlobAsync(string fileName, string newFileName);
        Task<IEnumerable<BlobItemWithMetadata>> SearchFilesAsync(string fileName);
        Task<bool> IsValidImageFile(IFormFile file);

    }
}
