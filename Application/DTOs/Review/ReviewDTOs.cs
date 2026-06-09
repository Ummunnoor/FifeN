using System;

namespace Application.DTOs.Review
{
    /// <summary>
    /// DTO for creating a product/shop review
    /// </summary>
    public class CreateReviewDTO
    {
        /// <summary>Shop being reviewed</summary>
        public Guid ShopId { get; set; }

        /// <summary>Optional: Specific product being reviewed</summary>
        public Guid? ProductId { get; set; }

        /// <summary>Order ID (proof of purchase)</summary>
        public Guid? OrderId { get; set; }

        /// <summary>Rating score (1-5 stars)</summary>
        public int Rating { get; set; }

        /// <summary>Review title/summary</summary>
        public string? Title { get; set; }

        /// <summary>Detailed review comment</summary>
        public required string Comment { get; set; }
    }

    /// <summary>
    /// DTO for retrieving a review
    /// </summary>
    public class GetReviewDTO
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public string? ReviewerName { get; set; }
        public int Rating { get; set; }
        public string? Title { get; set; }
        public string Comment { get; set; } = string.Empty;
        public bool IsVerifiedPurchase { get; set; }
        public int HelpfulCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? SellerResponse { get; set; }
        public bool HasSellerResponse { get; set; }
    }

    /// <summary>
    /// DTO for seller to respond to a review
    /// </summary>
    public class ReviewResponseDTO
    {
        public Guid ReviewId { get; set; }
        public required string SellerResponse { get; set; }
    }

    /// <summary>
    /// DTO for shop reviews summary
    /// </summary>
    public class ShopReviewsSummaryDTO
    {
        public Guid ShopId { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new(); // rating -> count
        public List<GetReviewDTO> RecentReviews { get; set; } = new();
    }
}
