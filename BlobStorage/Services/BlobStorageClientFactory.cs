using Azure.Storage.Blobs;

namespace BlobStorage.Services
{
    public class BlobStorageClientFactory : IBlobStorageClientFactory
    {
        private readonly string _connectionString;
        private readonly string _containerName;

        public BlobStorageClientFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BlobStorageConnectionString") ?? "connectDefault";
            _containerName = configuration.GetConnectionString("ContainerName") ?? "containerDefaultName";
        }

        public BlobContainerClient CreateBlobContainerClient()
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            return blobServiceClient.GetBlobContainerClient(_containerName);
        }
    }
}
