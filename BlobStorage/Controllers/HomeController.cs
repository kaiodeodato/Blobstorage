using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Sas;
using Azure.Storage.Blobs.Models;
using BlobStorage.Services;
using BlobStorage.Models;
using BlobStorage.Helpers;
using System.Drawing;

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

        public async Task<IActionResult> Index(string? fileName)
        {
            IEnumerable<BlobItemWithMetadata> files = new List<BlobItemWithMetadata>();

            try
            {
                if (!string.IsNullOrEmpty(fileName))
                {
                    files = await _blobService.SearchFilesAsync(fileName) ?? new List<BlobItemWithMetadata>();
                }
                else
                {
                    files = await _blobService.ListFilesAsync() ?? new List<BlobItemWithMetadata>();
                }
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = "Erro ao buscar arquivos. Tente novamente mais tarde.";
                TempData["ToastColor"] = "danger";
                Console.WriteLine($"Erro ao buscar arquivos: {ex.Message}");
            }

            return View(files); 
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile? file, [FromForm] string? description)
        {
            const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB em bytes

            string message;
            string color;

            if (file == null || file.Length == 0)
            {
                message = "Por favor, selecione um arquivo para fazer upload.";
                color = "warning";
            }
            else if (!(await _blobService.IsValidImageFile(file)))
            {
                message = "O arquivo não é uma imagem válida";
                color = "danger";
            }
            else if (file.Length > MAX_FILE_SIZE)
            {
                message = "O arquivo é muito grande! O tamanho máximo permitido é 5MB.";
                color = "warning";
            }
            else if (!file.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) &&
            !file.FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                message = "Somente arquivos JPEG ou JPG são permitidos.";
                color = "warning";
            }
            else if (string.IsNullOrEmpty(description)) 
            {
                message = "Por favor, insira uma descrição para o arquivo.";
                color = "warning";
            } 
            else
            {
                var result = await _blobService.UploadFileAsync(file, description);
                message = result ? "Upload realizado com sucesso!" : "Erro ao realizar o upload.";
                color = result ? "success" : "danger";
            }

            TempData["ToastMessage"] = message;
            TempData["ToastColor"] = color;

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
            var result = await _blobService.GenerateThumbnailAsync(fileName);

            return Redirect(result);
        }


        [HttpGet("show/{fileName}")]
        public async Task<IActionResult> ShowImage(string fileName)
        {
            var result = await _blobService.GenerateThumbnailAsync(fileName);

            return Content($"<html><body><img src='{result}' style='max-height:100%;'/></body></html>", "text/html");
        }

        [HttpPost("edit")]
        public async Task<IActionResult> EditName([FromForm] string fileName, [FromForm] string newFileName)
        {
            try
            {
                if (!newFileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) && !newFileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ToastMessage"] = "Somente arquivos JPEG ou JPG são permitidos.";
                    TempData["ToastColor"] = "warning";
                    return RedirectToAction("Index");
                }

                var result = await _blobService.RenameBlobAsync(fileName, newFileName);

                TempData["ToastMessage"] = result.Message;
                TempData["ToastColor"] = result.Success ? "success" : "danger";

            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Erro ao renomear o arquivo: {ex.Message}";
                TempData["ToastColor"] = "danger";
            }

            return RedirectToAction("Index");
        }


        [HttpPost("edit-description")]
        public async Task<IActionResult> EditDescription([FromForm] string fileName, [FromForm] string newDescription)
        {
            var success = await _blobService.EditBlobDescriptionAsync(fileName, newDescription);

            if (!success)
            {
                TempData["ToastMessage"] = $"Arquivo '{fileName}' não encontrado ou erro ao atualizar.";
                TempData["ToastColor"] = "danger";
                return RedirectToAction("Index");
            }

            TempData["ToastMessage"] = $"Descrição do arquivo '{fileName}' atualizada com sucesso!";
            TempData["ToastColor"] = "success";
            return RedirectToAction("Index");
        }


        [HttpPost]
        public IActionResult ToggleDisplayMode()
        {
            bool currentMode = (TempData["displayModeList"] as bool?) ?? true;
            TempData["displayModeList"] = !currentMode;

            return RedirectToAction("Index"); 
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                TempData["ToastMessage"] = "Por favor, insira um nome de arquivo para pesquisar.";
                TempData["ToastColor"] = "warning";
                return RedirectToAction("Index");
            }

            try
            {
                var files = await _blobService.SearchFilesAsync(fileName) ?? new List<BlobItemWithMetadata>();

                if (!files.Any())
                {
                    TempData["ToastMessage"] = $"Nenhum arquivo encontrado para '{fileName}'.";
                    TempData["ToastColor"] = "info";
                    return RedirectToAction("Index");
                }

                return RedirectToAction("Index", new { fileName = fileName });
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = "Erro ao buscar arquivos. Tente novamente mais tarde.";
                TempData["ToastColor"] = "danger";
                Console.WriteLine($"Erro ao buscar arquivos: {ex.Message}");
                return RedirectToAction("Index");
            }
        }





    }
}
