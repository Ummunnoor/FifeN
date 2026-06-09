using System;

namespace Application.DTOs.Shop
{
    /// <summary>
    /// DTO for retrieving shop details
    /// </summary>
    public class GetShopDTO : BaseShopDTO
    {
        /// <summary>Shop identifier</summary>
        public Guid Id { get; set; }

        /// <summary>Whether shop is verified</summary>
        public bool IsVerified { get; set; }

        /// <summary>Average rating (1-5)</summary>
        public decimal? AverageRating { get; set; }

        /// <summary>Total review count</summary>
        public int TotalReviews { get; set; }

        /// <summary>Number of products in shop</summary>
        public int ProductCount { get; set; }

        /// <summary>When shop was created</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Distance from user (populated by location search)</summary>
        public decimal? DistanceKm { get; set; }

        /// <summary>Whether the shop is open for business</summary>
        public bool IsActive { get; set; }
    }
}
