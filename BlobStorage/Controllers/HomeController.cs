using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Sas;

namespace BlobStorage.Controllers
{
    public class HomeController : Controller
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "containerblobs";

        public HomeController(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        // P�gina inicial com upload, download e exclus�o de arquivos
        public async Task<IActionResult> Index()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobItems = containerClient.GetBlobsAsync();

            var files = new List<Azure.Storage.Blobs.Models.BlobItem>();

            await foreach (var blobItem in blobItems)
            {
                files.Add(blobItem);
            }

            // Altere para IEnumerable<BlobItem> para suportar o LINQ
            return View(files.AsEnumerable());
        }

        // Upload de arquivo
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Arquivo inv�lido.");

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(file.FileName);
            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, true);

            return Ok(new { FileName = file.FileName, Url = blobClient.Uri.ToString() });
        }

        // Download de arquivo
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
                return NotFound("Arquivo n�o encontrado.");

            var download = await blobClient.DownloadAsync();
            return File(download.Value.Content, download.Value.ContentType, fileName);
        }

        // Excluir arquivo
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
                    TempData["Message"] = $"Arquivo '{fileName}' exclu�do com sucesso!";
                }
                else
                {
                    TempData["Message"] = $"Arquivo '{fileName}' n�o encontrado.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Erro ao excluir o arquivo: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Gerar URL SAS para exibir miniatura
        public async Task<IActionResult> GenerateThumbnail(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
            {
                return NotFound("Arquivo n�o encontrado.");
            }

            // Defina as permiss�es para a SAS (somente leitura)
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = fileName,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) // Defina o tempo de expira��o do SAS token
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // Gere a URL SAS
            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            // Retorne a URL da miniatura com a SAS token
            return Redirect(sasUri.ToString()); // Voc� pode ajustar o retorno conforme necess�rio
        }

        [HttpGet("show/{fileName}")]
        public async Task<IActionResult> ShowImage(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
                return NotFound("Imagem n�o encontrada.");

            // Gerar uma URL com SAS Token se o container for privado
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = fileName,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) // URL expira em 1 hora
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            // Retorna uma p�gina HTML para exibir a imagem
            return Content($"<html><body><img src='{sasUri}' style='max-width:100%;'/></body></html>", "text/html");
        }


    }
}
