using Azure.Storage.Blobs.Models;
using BlobStorage.Helpers;
using BlobStorage.Models;
using BlobStorage.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.ClientModel.Primitives;

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

        public async Task<bool> EditBlobDescriptionAsync(string fileName, string newDescription)
        {
            try
            {
                if (!await _blobRepository.BlobExistsAsync(fileName))
                {
                    return false;
                }

                newDescription = StringHelpers.RemoveNonAsciiCharacters(newDescription);

                var metadata = await _blobRepository.GetBlobMetadataAsync(fileName);
                metadata["Description"] = newDescription;

                await _blobRepository.SetBlobMetadataAsync(fileName, metadata);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string> GenerateThumbnailAsync(string fileName)
        {
            try
            {
                if (!await _blobRepository.BlobExistsAsync(fileName))
                {
                    return null;
                }
                return await _blobRepository.SetBlobSasPermition(fileName);
            }
            catch( Exception ex )
            {
                Console.WriteLine($"Erro ao gerar SAS para o arquivo {fileName}: {ex.Message}");
                return null;
            }
        }

        public async Task<string> ShowImageAsync(string fileName)
        {
            try
            {
                if (!await _blobRepository.BlobExistsAsync(fileName))
                {
                    return null;
                }
                return await _blobRepository.SetBlobSasPermition(fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao gerar SAS para o arquivo {fileName}: {ex.Message}");
                return null;
            }
        }

        public async Task<Models.OperationResult> RenameBlobAsync(string fileName, string newFileName)
        {
            try
            {
                if (fileName.Equals(newFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return new Models.OperationResult
                    {
                        Success = false,
                        Message = "O nome do arquivo já é o mesmo."
                    };
                }

                if (await _blobRepository.BlobExistsAsync(newFileName))
                {
                    return new Models.OperationResult
                    {
                        Success = false,
                        Message = $"Já existe um arquivo com o nome '{newFileName}'. Escolha outro nome."
                    };
                }

                if (!await _blobRepository.BlobExistsAsync(fileName))
                {
                    return new Models.OperationResult
                    {
                        Success = false,
                        Message = $"Arquivo '{fileName}' não encontrado."
                    };
                }

                var renameResult = await _blobRepository.DuplicateAndDeleteBlobAsync(fileName, newFileName);

                if (!renameResult)
                {
                    return new Models.OperationResult
                    {
                        Success = false,
                        Message = $"Erro ao renomear o arquivo '{fileName}'."
                    };
                }

                return new Models.OperationResult
                {
                    Success = true,
                    Message = $"Arquivo '{fileName}' renomeado para '{newFileName}' com sucesso!"
                };
            }
            catch (Exception ex)
            {
                return new Models.OperationResult
                {
                    Success = false,
                    Message = $"Erro ao renomear o arquivo: {ex.Message}"
                };
            }
        }


    }
}
