using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Persistence.Modules.Catalog
{
    /// <summary>
    /// Development image storage that does not persist bytes; it returns a deterministic placeholder
    /// URL so the listing flow works end to end. The production Cloudinary adapter (with f_auto,q_auto
    /// delivery) will replace this behind <see cref="IImageStorageService"/>.
    /// </summary>
    public sealed class DevImageStorageService(ILogger<DevImageStorageService> logger) : IImageStorageService
    {
        public Task<StoredImage> UploadAsync(ImageUpload image, string folder, CancellationToken ct)
        {
            var publicId = $"{folder}/{Guid.NewGuid():N}";
            var url = $"https://res.cloudinary.com/dev/image/upload/{publicId}";
            logger.LogInformation("[DEV IMAGE] Stored {File} ({Bytes} bytes) as {PublicId}.",
                image.FileName, image.Length, publicId);
            return Task.FromResult(new StoredImage(publicId, url));
        }

        public Task DeleteAsync(string publicId, CancellationToken ct)
        {
            logger.LogInformation("[DEV IMAGE] Deleted {PublicId}.", publicId);
            return Task.CompletedTask;
        }
    }
}
