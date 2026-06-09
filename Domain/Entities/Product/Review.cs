using System;
using Domain.Entities.Identity;
using Domain.Entities.Payment;

namespace Domain.Entities.Product
{
    /// <summary>
    /// Represents a review/rating from a buyer.
    /// Critical for trust system in the marketplace.
    /// Only verified purchases (completed orders) can be reviewed.
    /// </summary>
    public class Review
    {
        /// <summary>Primary key identifier</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Reference to the Shop being reviewed</summary>
        public Guid ShopId { get; set; }

        /// <summary>Navigation property to Shop</summary>
        public Shop? Shop { get; set; }

        /// <summary>Reference to the User who wrote the review (reviewer/buyer)</summary>
        public string ReviewerId { get; set; } = string.Empty;

        /// <summary>Navigation property to Reviewer</summary>
        public User? Reviewer { get; set; }

        /// <summary>Reference to the Order (proof of purchase)</summary>
        public Guid? OrderId { get; set; }

        /// <summary>Navigation property to Order</summary>
        public Order? Order { get; set; }

        /// <summary>Reference to specific Product reviewed (optional - can review entire order or specific item)</summary>
        public Guid? ProductId { get; set; }

        /// <summary>Navigation property to Product</summary>
        public Product? Product { get; set; }

        /// <summary>Rating score (1-5 stars)</summary>
        public int Rating { get; set; } // 1-5

        /// <summary>Review title/summary</summary>
        public string? Title { get; set; }

        /// <summary>Detailed review comment</summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>Whether this is a verified purchase (auto-set based on Order)</summary>
        public bool IsVerifiedPurchase { get; set; } = false;

        /// <summary>Whether shop has responded to the review</summary>
        public bool HasSellerResponse { get; set; } = false;

        /// <summary>Seller's response to the review</summary>
        public string? SellerResponse { get; set; }

        /// <summary>Helpful count (buyers marking as helpful)</summary>
        public int HelpfulCount { get; set; } = 0;

        /// <summary>When the review was created</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When the review was last updated</summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Whether review is visible (for moderation purposes)</summary>
        public bool IsActive { get; set; } = true;
    }
}
