using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Sas;
using Azure.Storage.Blobs.Models;
using BlobStorage.Services;
using BlobStorage.Models;
using BlobStorage.Helpers;

namespace BlobStorage.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class HomeController : Controller
    {
        private readonly IBlobStorageClientFactory _blobStorageClientFactory;
        private readonly string _containerName;
        private readonly IBlobService _blobService;

        public HomeController(IBlobService blobService, IBlobStorageClientFactory blobStorageClientFactory, IConfiguration configuration)
        {
            _blobStorageClientFactory = blobStorageClientFactory;
            _containerName = configuration.GetConnectionString("ContainerName") ?? "defaultContainerName";

            _blobService = blobService;
        }

        public async Task<IActionResult> Index()
        {
            var files = await _blobService.ListFilesAsync();
            return View(files);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] string description)
        {
            if (file == null || file.Length == 0)
                return BadRequest("invalid file.");

            var result = await _blobService.UploadFileAsync(file, description);

            TempData["ToastMessage"] = result ? "Upload Success" : "Upload Error";
            TempData["ToastColor"] = result ? "success" : "danger";

            return RedirectToAction("Index");
        }

        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            var file = await _blobService.DownloadFileAsync(fileName);

            if (file == null)
                return NotFound("File not found");

            return File(file.Content, file.ContentType, fileName);
        }

        [HttpPost("delete/{fileName}")]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            var result = await _blobService.DeleteFileAsync(fileName);

            TempData["ToastMessage"] = result ? $"Arquivo '{fileName}' excluído com sucesso!" : $"Arquivo '{fileName}' não encontrado.";
            TempData["ToastColor"] = result ? "success" : "danger";

            return RedirectToAction("Index");
        }

        [HttpGet("generate-thumbnail/{fileName}")]
        public async Task<IActionResult> GenerateThumbnail(string fileName)
        {
            var blobContainerClient = _blobStorageClientFactory.CreateBlobContainerClient();

            var blobClient = blobContainerClient.GetBlobClient(fileName);

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
            var blobContainerClient = _blobStorageClientFactory.CreateBlobContainerClient();

            var blobClient = blobContainerClient.GetBlobClient(fileName);

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
        public async Task<IActionResult> EditName([FromForm] string fileName, [FromForm] string newFileName)
        {
            try
            {
                if (fileName.Equals(newFileName, StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ToastMessage"] = "O nome do arquivo já é o mesmo.";
                    TempData["ToastColor"] = "danger";
                    return RedirectToAction("Index");
                }

                var blobContainerClient = _blobStorageClientFactory.CreateBlobContainerClient();

                // Verificar se já existe um arquivo com o novo nome
                var blobExists = false;
                await foreach (var blobItem in blobContainerClient.GetBlobsAsync())
                {
                    if (blobItem.Name.Equals(newFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        blobExists = true;
                        break;
                    }
                }

                if (blobExists)
                {
                    TempData["ToastMessage"] = $"Já existe um arquivo com o nome '{newFileName}'. Escolha outro nome.";
                    TempData["ToastColor"] = "danger";
                    return RedirectToAction("Index");
                }

                var sourceBlobClient = blobContainerClient.GetBlobClient(fileName);
                var destinationBlobClient = blobContainerClient.GetBlobClient(newFileName);

                if (!await sourceBlobClient.ExistsAsync())
                {
                    TempData["Message"] = $"Arquivo '{fileName}' não encontrado.";
                    return RedirectToAction("Index");
                }

                await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);

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

                await sourceBlobClient.DeleteIfExistsAsync();

                TempData["ToastMessage"] = $"Arquivo '{fileName}' renomeado para '{newFileName}' com sucesso!";
                TempData["ToastColor"] = "success";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Erro ao renomear o arquivo: {ex.Message}";
            }

            return RedirectToAction("Index");
        }


        [HttpPost("edit-description")]
        public async Task<IActionResult> EditDescription([FromForm] string fileName, [FromForm] string newDescription)
        {
            try
            {
                var blobContainerClient = _blobStorageClientFactory.CreateBlobContainerClient();
                var blobClient = blobContainerClient.GetBlobClient(fileName);

                if (!await blobClient.ExistsAsync())
                {
                    TempData["ToastMessage"] = $"Arquivo '{fileName}' não encontrado.";
                    TempData["ToastColor"] = "danger";
                    return RedirectToAction("Index");
                }

                newDescription = StringHelpers.RemoveNonAsciiCharacters(newDescription);

                var metadata = await blobClient.GetPropertiesAsync();
                metadata.Value.Metadata["Description"] = newDescription;

                await blobClient.SetMetadataAsync(metadata.Value.Metadata);

                TempData["ToastMessage"] = $"Descrição do arquivo '{fileName}' atualizada com sucesso!";
                TempData["ToastColor"] = "success";
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Erro ao atualizar a descrição: {ex.Message}";
                TempData["ToastColor"] = "danger";
            }

            return RedirectToAction("Index");
        }


        [HttpPost]
        public IActionResult ToggleDisplayMode()
        {
            bool currentMode = (TempData["displayModeList"] as bool?) ?? true;
            TempData["displayModeList"] = !currentMode;

            return RedirectToAction("Index"); 
        }


    }
}
