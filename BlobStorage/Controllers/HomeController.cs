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


    }
}
