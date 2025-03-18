using Azure.Storage.Blobs;

namespace BlobStorage.Services
{
    public interface IBlobStorageClientFactory
    {
        BlobContainerClient CreateBlobContainerClient();
    }
}
