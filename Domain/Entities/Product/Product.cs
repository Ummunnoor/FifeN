using System;
using System.Collections.Generic;

namespace Domain.Entities.Product
{
    /// <summary>
    /// Represents a product listing in the marketplace.
    /// Products belong to a Shop (vendor).
    /// </summary>
    public class Product
    {
        /// <summary>Primary key identifier</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Reference to the Shop selling this product</summary>
        public Guid ShopId { get; set; }

        /// <summary>Navigation property to Shop</summary>
        public Shop? Shop { get; set; }
        /// <summary>Product name/title</summary>
        public required string Name { get; set; }

        /// <summary>Detailed product description</summary>
        public required string Description { get; set; }

        /// <summary>Product price</summary>
        public decimal Price { get; set; }

        /// <summary>Reference to product category</summary>
        public Guid CategoryId { get; set; }

        /// <summary>Navigation property to Category</summary>
        public Category? Category { get; set; }

        /// <summary>Available quantity in stock</summary>
        public int StockQuantity { get; set; } = 0;

        /// <summary>Minimum quantity that can be ordered</summary>
        public int MinimumOrderQuantity { get; set; } = 1;

        /// <summary>Is this product currently available for purchase</summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>Product status visibility (Active, Inactive, Draft)</summary>
        public string Status { get; set; } = "Active"; // Active, Inactive, Draft

        /// <summary>Average rating cached from reviews</summary>
        public decimal? AverageRating { get; set; }

        /// <summary>Total review count cached for performance</summary>
        public int TotalReviews { get; set; } = 0;

        /// <summary>Number of times this product has been viewed</summary>
        public int ViewCount { get; set; } = 0;

        /// <summary>When the product was created</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When the product was last updated</summary>
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
