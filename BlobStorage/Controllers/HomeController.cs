using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;

namespace BlobStorage.Controllers
{
    public class HomeController : Controller
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public HomeController(BlobServiceClient blobServiceClient, IConfiguration configuration)
        {
            _blobServiceClient = blobServiceClient;
            //_containerName = configuration.GetConnectionString("ContainerName") ?? "defaultContainerName";
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

            // Defina as permissões para a SAS (somente leitura)
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = fileName,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) // Defina o tempo de expiração do SAS token
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // Gere a URL SAS
            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            // Retorne a URL da miniatura com a SAS token
            return Redirect(sasUri.ToString()); // Você pode ajustar o retorno conforme necessário
        }

        [HttpGet("show/{fileName}")]
        public async Task<IActionResult> ShowImage(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
                return NotFound("Imagem não encontrada.");

            // Gerar uma URL com SAS Token se o container for privado
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = fileName,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) // URL expira em 1 hora
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            // Retorna uma página HTML para exibir a imagem
            return Content($"<html><body><img src='{sasUri}' style='max-width:100%;'/></body></html>", "text/html");
        }


    }
}
