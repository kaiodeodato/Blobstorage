using Azure.Storage.Blobs.Models;
using BlobStorage.Models;
using BlobStorage.Repositories;

namespace BlobStorage.Services
{
    public class BlobService : IBlobService
    {
        private readonly IBlobRepository _blobRepository;

        public BlobService(IBlobRepository blobRepository)
        {
            _blobRepository = blobRepository;
        }

        public async Task<IEnumerable<BlobItemWithMetadata>> ListFilesAsync()
        {
            return await _blobRepository.GetAllBlobsAsync();
        }

        public async Task<bool> UploadFileAsync(IFormFile file, string description)
        {
            try
            {
                await _blobRepository.UploadBlobAsync(file, description);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<BlobDownloadInfo?> DownloadFileAsync(string fileName)
        {
            return await _blobRepository.DownloadBlobAsync(fileName);
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            try
            {
                return await _blobRepository.DeleteBlobAsync(fileName);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Erro ao excluir arquivo: {ex.Message}");
                return false;
            }
        }
    }
}
