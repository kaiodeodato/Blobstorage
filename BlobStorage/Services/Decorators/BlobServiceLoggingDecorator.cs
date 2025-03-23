using BlobStorage.Models;
using System.Diagnostics;
using BlobStorage.Services;
using Azure.Storage.Blobs.Models;

namespace BlobStorage.Services.Decorators
{
    public class BlobServiceLoggingDecorator : IBlobService
    {
        private readonly IBlobService _innerService;
        private readonly ILogger<BlobServiceLoggingDecorator> _logger;

        public BlobServiceLoggingDecorator(IBlobService innerService, ILogger<BlobServiceLoggingDecorator> logger)
        {
            _innerService = innerService;
            _logger = logger;
        }

        private async Task<T> LogExecutionAsync<T>(Func<Task<T>> action, string methodName)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await action();
                stopwatch.Stop();
                _logger.LogInformation($"{methodName} completed in {stopwatch.ElapsedMilliseconds} ms.");
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError($"{methodName} failed after {stopwatch.ElapsedMilliseconds} ms. Error: {ex.Message}");
                throw;
            }
        }

        public Task<IEnumerable<BlobItemWithMetadata>> ListFilesAsync() =>
            LogExecutionAsync(() => _innerService.ListFilesAsync(), nameof(ListFilesAsync));

        public Task<bool> UploadFileAsync(IFormFile file, string description) =>
            LogExecutionAsync(() => _innerService.UploadFileAsync(file, description), nameof(UploadFileAsync));

        public Task<BlobDownloadInfo?> DownloadFileAsync(string fileName) =>
            LogExecutionAsync(() => _innerService.DownloadFileAsync(fileName), nameof(DownloadFileAsync));

        public Task<bool> DeleteFileAsync(string fileName) =>
            LogExecutionAsync(() => _innerService.DeleteFileAsync(fileName), nameof(DeleteFileAsync));

        public Task<bool> EditBlobDescriptionAsync(string fileName, string newDescription) =>
            LogExecutionAsync(() => _innerService.EditBlobDescriptionAsync(fileName, newDescription), nameof(EditBlobDescriptionAsync));

        public Task<string> GenerateThumbnailAsync(string fileName) =>
            LogExecutionAsync(() => _innerService.GenerateThumbnailAsync(fileName), nameof(GenerateThumbnailAsync));

        public Task<string> ShowImageAsync(string fileName) =>
            LogExecutionAsync(() => _innerService.ShowImageAsync(fileName), nameof(ShowImageAsync));

        public Task<Models.OperationResult> RenameBlobAsync(string fileName, string newFileName) =>
            LogExecutionAsync(() => _innerService.RenameBlobAsync(fileName, newFileName), nameof(RenameBlobAsync));

        public Task<IEnumerable<BlobItemWithMetadata>> SearchFilesAsync(string fileName) =>
            LogExecutionAsync(() => _innerService.SearchFilesAsync(fileName), nameof(SearchFilesAsync));

        public Task<bool> IsValidImageFile(IFormFile file) =>
            LogExecutionAsync(() => _innerService.IsValidImageFile(file), nameof(IsValidImageFile));

    }

}
