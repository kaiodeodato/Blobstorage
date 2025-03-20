﻿using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BlobStorage.Models;
using BlobStorage.Services;

namespace BlobStorage.Repositories
{
    public class BlobRepository : IBlobRepository
    {
        private readonly IBlobStorageClientFactory _blobStorageClientFactory;

        public BlobRepository(IBlobStorageClientFactory blobStorageClientFactory)
        {
            _blobStorageClientFactory = blobStorageClientFactory;
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
    }
}
