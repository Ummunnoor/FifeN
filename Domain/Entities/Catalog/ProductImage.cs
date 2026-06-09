using System;

namespace Domain.Entities.Catalog
{
    /// <summary>
    /// An image attached to a listing, stored on Cloudinary. The first image (lowest
    /// <see cref="SortOrder"/>, or <see cref="IsCover"/>) is the cover used in summaries.
    /// </summary>
    public class ProductImage
    {
        public Guid Id { get; set; }

        /// <summary>Owning product.</summary>
        public Guid ProductId { get; set; }

        /// <summary>Cloudinary public id, used to transform or delete the asset.</summary>
        public string CloudinaryPublicId { get; set; } = default!;

        /// <summary>Delivery URL (served via Cloudinary's f_auto,q_auto CDN).</summary>
        public string Url { get; set; } = default!;

        /// <summary>Whether this image is the listing cover.</summary>
        public bool IsCover { get; set; }

        /// <summary>Display order within the listing's gallery.</summary>
        public int SortOrder { get; set; }

        /// <summary>When the image was uploaded (UTC).</summary>
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
