
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using crudEbancoDist8.Interface;

namespace crudEbancoDist8.Services;

public class CloudinaryImageStorageService : IImageStorageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryImageStorageService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(apiSecret))
        {
            throw new InvalidOperationException(
                "Configuração do Cloudinary incompleta.");
        }

        var account = new Account(
            cloudName,
            apiKey,
            apiSecret);

        _cloudinary = new Cloudinary(account);
    }

    public async Task<StoredImageResult> UploadAsync(
    IFormFile file,
        CancellationToken cancellationToken = default)
    {
        const long maxSize = 5 * 1024 * 1024;

        if (file.Length == 0 || file.Length > maxSize)
        {
            throw new ArgumentException(
                "A imagem deve ter entre 1 byte e 5 MB.");
        }

        var allowedTypes = new[]
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        if (!allowedTypes.Contains(file.ContentType))
        {
            throw new ArgumentException(
                "Formato inválido. Use JPG, PNG ou WebP.");
        }

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(
                file.FileName,
                stream),

            Folder = "codigo-dos-bares/produtos",
            UseFilename = false,
            UniqueFilename = true
        };

        var result = await _cloudinary.UploadAsync(
            uploadParams,
            cancellationToken);

        if (result.Error != null ||
            string.IsNullOrWhiteSpace(result.SecureUrl?.ToString()) ||
            string.IsNullOrWhiteSpace(result.PublicId))
        {
            throw new InvalidOperationException(
                "Falha ao enviar imagem para o Cloudinary.");
        }

        return new StoredImageResult(
            result.SecureUrl.ToString(),
            result.PublicId);
    }




    public async Task DeleteAsync(
        string publicId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            throw new ArgumentException(
                "O identificador da imagem é obrigatório.",
                nameof(publicId));

        cancellationToken.ThrowIfCancellationRequested();

        var deleteParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image
        };

        var result = await _cloudinary.DestroyAsync(deleteParams);

        if (result.Error != null ||
            (result.Result != "ok" && result.Result != "not found"))
        {
            throw new InvalidOperationException(
                "Falha ao excluir imagem do Cloudinary.");
        }
    }


}
