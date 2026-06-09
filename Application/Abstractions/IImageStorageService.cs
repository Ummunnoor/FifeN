using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Abstractions
{
    /// <summary>A single image to upload, decoupled from any web framework type.</summary>
    public record ImageUpload(Stream Content, string FileName, string ContentType, long Length);

    /// <summary>The stored asset's provider id and delivery URL.</summary>
    public record StoredImage(string PublicId, string Url);

    /// <summary>
    /// Port over the image host (Cloudinary in production, with f_auto,q_auto CDN delivery). Keeps the
    /// concrete provider out of the application and API layers.
    /// </summary>
    public interface IImageStorageService
    {
        Task<StoredImage> UploadAsync(ImageUpload image, string folder, CancellationToken ct);

        Task DeleteAsync(string publicId, CancellationToken ct);
    }
}
