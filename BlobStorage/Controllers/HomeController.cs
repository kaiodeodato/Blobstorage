using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs.Models;

namespace BlobStorage.Controllers
{
    public class HomeController : Controller
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public HomeController(BlobServiceClient blobServiceClient, IConfiguration configuration)
        {
            _blobServiceClient = blobServiceClient;
            _containerName = configuration.GetConnectionString("ContainerName") ?? "defaultContainerName";
        }

        public async Task<IActionResult> Index()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobItems = containerClient.GetBlobsAsync();

            var files = new List<Azure.Storage.Blobs.Models.BlobItem>();

            await foreach (var blobItem in blobItems)
            {
                files.Add(blobItem);
            }

            return View(files.AsEnumerable());
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("invalid file.");

            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync();

                var blobClient = containerClient.GetBlobClient(file.FileName);
                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, true);

                TempData["Message"] = "Upload Success";
                TempData["MessageType"] = "success";
                return RedirectToAction("Index"); 
            }
            catch 
            {
                TempData["Message"] = "Upload Error";
                TempData["MessageType"] = "error";
                return RedirectToAction("Index");
            }
        }

        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
                return NotFound("File not found");

            var download = await blobClient.DownloadAsync();
            return File(download.Value.Content, download.Value.ContentType, fileName);
        }

        [HttpPost("delete/{fileName}")]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                Console.WriteLine($"Tentando deletar: {fileName}");

                var response = await blobClient.DeleteIfExistsAsync();

                if (response)
                {
                    TempData["Message"] = $"Arquivo '{fileName}' excluído com sucesso!";
                }
                else
                {
                    TempData["Message"] = $"Arquivo '{fileName}' não encontrado.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Erro ao excluir o arquivo: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> GenerateThumbnail(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
            {
                return NotFound("Arquivo não encontrado.");
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = fileName,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            return Redirect(sasUri.ToString());
        }

        [HttpGet("show/{fileName}")]
        public async Task<IActionResult> ShowImage(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
                return NotFound("Imagem não encontrada.");

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = fileName,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) 
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            return Content($"<html><body><img src='{sasUri}' style='max-height:100%;'/></body></html>", "text/html");
        }

        [HttpPost("edit")]
        public async Task<IActionResult> EditName(string fileName, string newFileName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                var sourceBlobClient = containerClient.GetBlobClient(fileName);
                var destinationBlobClient = containerClient.GetBlobClient(newFileName);

                // Verifica se o arquivo original existe
                if (!await sourceBlobClient.ExistsAsync())
                {
                    TempData["Message"] = $"Arquivo '{fileName}' não encontrado.";
                    return RedirectToAction("Index");
                }

                // Copia o arquivo para um novo blob
                await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);

                // Aguarda a cópia ser concluída
                BlobProperties properties;
                do
                {
                    await Task.Delay(500);
                    properties = await destinationBlobClient.GetPropertiesAsync();
                } while (properties.CopyStatus == CopyStatus.Pending);

                if (properties.CopyStatus != CopyStatus.Success)
                {
                    TempData["Message"] = $"Erro ao copiar o arquivo '{fileName}'.";
                    return RedirectToAction("Index");
                }

                // Exclui o arquivo original
                await sourceBlobClient.DeleteIfExistsAsync();

                TempData["Message"] = $"Arquivo '{fileName}' renomeado para '{newFileName}' com sucesso!";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Erro ao renomear o arquivo: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}
