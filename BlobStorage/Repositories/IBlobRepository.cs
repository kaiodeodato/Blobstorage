using Azure.Storage.Blobs.Models;
using BlobStorage.Models;

namespace BlobStorage.Repositories
{
    public interface IBlobRepository
    {
        Task<IEnumerable<BlobItemWithMetadata>> GetAllBlobsAsync();
        Task UploadBlobAsync(IFormFile file, string description);
        Task<BlobDownloadInfo> DownloadBlobAsync(string fileName);
        Task<bool> DeleteBlobAsync(string fileName);
        Task<bool> BlobExistsAsync(string fileName);
        Task<IDictionary<string, string>> GetBlobMetadataAsync(string fileName);
        Task SetBlobMetadataAsync(string fileName, IDictionary<string, string> metadata);
        Task<string> SetBlobSasPermition(string fileName);
        Task<bool> DuplicateAndDeleteBlobAsync(string fileName, string newFileName);
        Task<IEnumerable<BlobItemWithMetadata>> SearchBlobsAsync(string fileName);
    }
}
