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

    }
}
