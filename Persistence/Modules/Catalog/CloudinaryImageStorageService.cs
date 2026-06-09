using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;

namespace Persistence.Modules.Catalog
{
    /// <summary>
    /// Production image storage backed by Cloudinary. Uploads are stored under a per-listing folder
    /// and delivered through the CDN with <c>f_auto,q_auto</c> (automatic format + quality) for the
    /// low-bandwidth targets in the NFRs.
    /// </summary>
    public sealed class CloudinaryImageStorageService(
        Cloudinary cloudinary, ILogger<CloudinaryImageStorageService> logger) : IImageStorageService
    {
        public async Task<StoredImage> UploadAsync(ImageUpload image, string folder, CancellationToken ct)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(image.FileName, image.Content),
                Folder = folder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            };

            var result = await cloudinary.UploadAsync(uploadParams, ct);
            if (result.Error is not null)
            {
                logger.LogError("Cloudinary upload failed for {File}: {Error}",
                    image.FileName, result.Error.Message);
                throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");
            }

            // Deliver via the CDN with automatic format/quality rather than the raw upload URL.
            var url = cloudinary.Api.UrlImgUp
                .Secure(true)
                .Transform(new Transformation().Quality("auto").FetchFormat("auto"))
                .BuildUrl($"{result.PublicId}.{result.Format}");

            logger.LogInformation("Cloudinary stored {File} as {PublicId}.", image.FileName, result.PublicId);
            return new StoredImage(result.PublicId, url);
        }

        public async Task DeleteAsync(string publicId, CancellationToken ct)
        {
            var result = await cloudinary.DestroyAsync(new DeletionParams(publicId));
            if (!string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(result.Result, "not found", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Cloudinary delete for {PublicId} returned '{Result}'.", publicId, result.Result);
            }
        }
    }
}
