using System;
using System.Collections.Generic;
using Domain.Entities.Enums;
using Domain.Entities.Reviews;
using Domain.Entities.Vendors;
using Domain.ValueObjects;
using NpgsqlTypes;

namespace Domain.Entities.Catalog
{
    /// <summary>
    /// A vendor's product listing. Soft-deleted via <see cref="Status"/> (never physically removed);
    /// only <see cref="ListingStatus.Live"/> listings from approved, active vendors are publicly visible.
    /// </summary>
    public class Product
    {
        public Guid Id { get; set; }

        /// <summary>Owning vendor.</summary>
        public Guid VendorProfileId { get; set; }
        public VendorProfile Vendor { get; set; } = default!;

        /// <summary>Category (single category per product in the MVP).</summary>
        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = default!;

        /// <summary>Listing title (max 80 characters).</summary>
        public string Title { get; set; } = default!;

        /// <summary>Listing description (max 1000 characters).</summary>
        public string Description { get; set; } = default!;

        /// <summary>Price in NGN; owned value object.</summary>
        public Money Price { get; set; } = default!;

        /// <summary>How the price should be read (Fixed / Negotiable / ContactForPrice).</summary>
        public PriceType PriceType { get; set; } = PriceType.Fixed;

        /// <summary>Item condition; constrained by the category's consumable flag.</summary>
        public ProductCondition Condition { get; set; }

        /// <summary>State + city; owned value object. No geospatial data in the MVP.</summary>
        public Location Location { get; set; } = default!;

        /// <summary>Visibility lifecycle state.</summary>
        public ListingStatus Status { get; set; } = ListingStatus.Live;

        /// <summary>Free-form, category-specific attributes, stored as jsonb.</summary>
        public Dictionary<string, string> Attributes { get; set; } = new();

        /// <summary>Running view count, incremented asynchronously on detail reads.</summary>
        public int ViewCount { get; set; }

        /// <summary>Generated full-text search vector over title + description (tsvector + GIN).</summary>
        public NpgsqlTsVector? SearchVector { get; set; }

        /// <summary>When the listing was created (UTC).</summary>
        public DateTimeOffset CreatedAtUtc { get; set; }

        /// <summary>When the listing was last updated (UTC). Drives "active listing" freshness.</summary>
        public DateTimeOffset UpdatedAtUtc { get; set; }

        // Navigation properties
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
