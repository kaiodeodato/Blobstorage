using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using BlobStorage.Helpers;
using BlobStorage.Models;
using BlobStorage.Services;
using Microsoft.Extensions.Configuration;

namespace BlobStorage.Repositories
{
    public class BlobRepository : IBlobRepository
    {
        private readonly IBlobStorageClientFactory _blobStorageClientFactory;
        private readonly string _containerName;
        public BlobRepository(IBlobStorageClientFactory blobStorageClientFactory, IConfiguration configuration)
        {
            _blobStorageClientFactory = blobStorageClientFactory;
            _containerName = configuration.GetConnectionString("ContainerName") ?? "defaultContainerName";
        }

        public async Task<IEnumerable<BlobItemWithMetadata>> GetAllBlobsAsync()
        {
            var blobContainerClient = _blobStorageClientFactory.CreateBlobContainerClient();
            var blobItems = blobContainerClient.GetBlobsAsync();

            var files = new List<BlobItemWithMetadata>();

            await foreach (var blobItem in blobItems)
            {
                var blobClient = blobContainerClient.GetBlobClient(blobItem.Name);
                var properties = await blobClient.GetPropertiesAsync();
                var metadata = properties.Value.Metadata;

                var description = metadata.ContainsKey("Description") ? metadata["Description"] : "Sem descrição";

                files.Add(new BlobItemWithMetadata { Name = blobItem.Name, Description = description });
            }

            return files;
        }

        public async Task UploadBlobAsync(IFormFile file, string description)
        {
            var blobContainerClient = _blobStorageClientFactory.CreateBlobContainerClient();
            await blobContainerClient.CreateIfNotExistsAsync();

            var blobClient = blobContainerClient.GetBlobClient(file.FileName);
            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, true);

            var metadata = new Dictionary<string, string>
        {
            { "Description", description }
        };
            await blobClient.SetMetadataAsync(metadata);
        }

        public async Task<BlobDownloadInfo?> DownloadBlobAsync(string fileName)
        {
            var blobContainerClient = _blobStorageClientFactory.CreateBlobContainerClient();
            var blobClient = blobContainerClient.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
                return null;

            return await blobClient.DownloadAsync();
        }

        public async Task<bool> DeleteBlobAsync(string fileName)
        {
            var blobContainerClient = _blobStorageClientFactory.CreateBlobContainerClient();
            var blobClient = blobContainerClient.GetBlobClient(fileName);

            return await blobClient.DeleteIfExistsAsync();
        }

        public async Task<bool> BlobExistsAsync(string fileName)
        {
            var blobContainerClient = _blobStorageClientFactory.CreateBlobContainerClient();
            var blobClient = blobContainerClient.GetBlobClient(fileName);
            return await blobClient.ExistsAsync();
        }

        public async Task<IDictionary<string, string>> GetBlobMetadataAsync(string fileName)
        {
            var blobContainerClient = _blobStorageClientFactory.CreateBlobContainerClient();
            var blobClient = blobContainerClient.GetBlobClient(fileName);
            var properties = await blobClient.GetPropertiesAsync();
            return properties.Value.Metadata;
        }

        public async Task SetBlobMetadataAsync(string fileName, IDictionary<string, string> metadata)
        {
            var blobContainerClient = _blobStorageClientFactory.CreateBlobContainerClient();
            var blobClient = blobContainerClient.GetBlobClient(fileName);
            await blobClient.SetMetadataAsync(metadata);
        }
        public async Task<string> SetBlobSasPermition(string fileName)
        {
            var blobContainerClient = _blobStorageClientFactory.CreateBlobContainerClient();
            var blobClient = blobContainerClient.GetBlobClient(fileName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = fileName,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            return sasUri.ToString();
        }
        public async Task<bool> DuplicateAndDeleteBlobAsync(string fileName, string newFileName)
        {
            try
            {
                var blobContainerClient = _blobStorageClientFactory.CreateBlobContainerClient();

                var sourceBlobClient = blobContainerClient.GetBlobClient(fileName);
                var destinationBlobClient = blobContainerClient.GetBlobClient(newFileName);

                await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);

                BlobProperties properties;
                do
                {
                    await Task.Delay(500);
                    properties = await destinationBlobClient.GetPropertiesAsync();
                } while (properties.CopyStatus == CopyStatus.Pending);

                if (properties.CopyStatus != CopyStatus.Success)
                {
                    return false;
                }

                await sourceBlobClient.DeleteIfExistsAsync();

                return true; 
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
