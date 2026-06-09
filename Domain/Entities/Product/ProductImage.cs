using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Product
{
    /// <summary>
    /// Represents images/photos of a product.
    /// Supports multiple images per product for marketplace appeal.
    /// Images should be stored in cloud storage (S3, Cloudinary, etc) with URLs in database.
    /// </summary>
    public class ProductImage
    {
        /// <summary>Primary key identifier</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Reference to the Product</summary>
        public Guid ProductId { get; set; }

        /// <summary>Navigation property to Product</summary>
        public Product? Product { get; set; }

        /// <summary>URL to the full-size image in cloud storage</summary>
        [Required]
        public required string ImageUrl { get; set; }

        /// <summary>URL to thumbnail/compressed version (for list views, faster loading)</summary>
        public string? ThumbnailUrl { get; set; }

        /// <summary>Alternative text (for accessibility and SEO)</summary>
        public string? AltText { get; set; }

        /// <summary>Display order of images (primary image first)</summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>Whether this is the primary/featured image</summary>
        public bool IsPrimary { get; set; } = false;

        /// <summary>File size in bytes (for bandwidth monitoring)</summary>
        public long FileSizeBytes { get; set; }

        /// <summary>Original file name (for reference)</summary>
        public string? OriginalFileName { get; set; }

        /// <summary>Image MIME type (image/jpeg, image/png, etc)</summary>
        public string? MimeType { get; set; }

        /// <summary>Upload status: Pending, Uploaded, Failed</summary>
        public string UploadStatus { get; set; } = "Pending"; // Pending, Uploaded, Failed

        /// <summary>When the image was uploaded</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Cloud storage key/path (for deletion and management)</summary>
        public string? StorageKey { get; set; }
    }
}
