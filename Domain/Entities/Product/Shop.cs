using Domain.Entities.Identity;
using Domain.Entities.Payment;

namespace Domain.Entities.Product
{
    /// <summary>
    /// Represents a vendor's shop/storefront in the marketplace.
    /// One user can have exactly one shop (after vendor approval).
    /// </summary>
    public class Shop
    {
        /// <summary>Primary key identifier</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Reference to the User who owns this shop</summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>Navigation property to User</summary>
        public User? User { get; set; }

        /// <summary>Shop display name/business name</summary>
        public required string Name { get; set; }

        /// <summary>Shop description</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Shop profile image URL</summary>
        public string? ImageUrl { get; set; }

        /// <summary>Latitude for location-based discovery</summary>
        public decimal? Latitude { get; set; }

        /// <summary>Longitude for location-based discovery</summary>
        public decimal? Longitude { get; set; }

        /// <summary>Shop address for reference</summary>
        public string? Address { get; set; }

        /// <summary>Shop contact phone number (can be WhatsApp)</summary>
        public string? PhoneNumber { get; set; }

        /// <summary>Timestamp when vendor was verified</summary>
        public DateTime? VerifiedAt { get; set; }

        /// <summary>Average rating (cached from reviews for performance)</summary>
        public decimal? AverageRating { get; set; }

        /// <summary>Total number of reviews cached for performance</summary>
        public int TotalReviews { get; set; } = 0;

        /// <summary>When the shop was created</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When the shop was last updated</summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Soft delete - shop deactivation</summary>
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
