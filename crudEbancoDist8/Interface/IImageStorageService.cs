
using Microsoft.AspNetCore.Http;

namespace crudEbancoDist8.Interface;

public interface IImageStorageService
{
    Task<StoredImageResult> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
    string publicId,
    CancellationToken cancellationToken = default);
}

public record StoredImageResult(
    string Url,
    string PublicId);
