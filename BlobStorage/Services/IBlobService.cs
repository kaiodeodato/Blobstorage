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
    }
}
