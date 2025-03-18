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


        public HomeController(IBlobStorageClientFactory blobStorageClientFactory, IConfiguration configuration)
        {
            _blobStorageClientFactory = blobStorageClientFactory;
            _containerName = configuration.GetConnectionString("ContainerName") ?? "defaultContainerName";
        }

        public async Task<IActionResult> Index()
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

                var fileWithMetadata = new BlobItemWithMetadata
                {
                    Name = blobItem.Name,
                    Description = description
                };

                files.Add(fileWithMetadata);
            }

            return View(files.AsEnumerable());
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] string description)
        {
            if (file == null || file.Length == 0)
                return BadRequest("invalid file.");

            try
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

                TempData["ToastMessage"] = "Upload Success";
                TempData["ToastColor"] = "success";
                return RedirectToAction("Index");
            }
            catch
            {
                TempData["ToastMessage"] = "Upload Error";
                TempData["ToastColor"] = "danger";
                return RedirectToAction("Index");
            }
        }


        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            var blobContainerClient = _blobStorageClientFactory.CreateBlobContainerClient();

            var blobClient = blobContainerClient.GetBlobClient(fileName);

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
                var blobContainerClient = _blobStorageClientFactory.CreateBlobContainerClient();


                var blobClient = blobContainerClient.GetBlobClient(fileName);

                Console.WriteLine($"Tentando deletar: {fileName}");

                var response = await blobClient.DeleteIfExistsAsync();

                if (response)
                {
                    TempData["ToastMessage"] = $"Arquivo '{fileName}' excluído com sucesso!";
                    TempData["ToastColor"] = "success";
                }
                else
                {
                    TempData["ToastMessage"] = $"Arquivo '{fileName}' não encontrado.";
                    TempData["ToastColor"] = "danger";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Erro ao excluir o arquivo: {ex.Message}";
                TempData["ToastColor"] = "danger";
                return RedirectToAction("Index");
            }
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
            // Alterna o valor da variável
            bool currentMode = (TempData["displayModeList"] as bool?) ?? true;
            TempData["displayModeList"] = !currentMode;

            return RedirectToAction("Index"); // Redireciona para a Home
        }


    }
}
