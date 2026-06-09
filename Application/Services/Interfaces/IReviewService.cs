using Application.DTOs;
using Application.DTOs.Review;

namespace Application.Services.Interfaces
{
    /// <summary>
    /// Service for managing product and shop reviews/ratings
    /// </summary>
    public interface IReviewService
    {
        /// <summary>
        /// Create a new review for a shop (verified purchase required)
        /// </summary>
        Task<BaseResponse<GetReviewDTO>> CreateReviewAsync(CreateReviewDTO createReviewDTO, string reviewerId);

        /// <summary>
        /// Get a specific review
        /// </summary>
        Task<BaseResponse<GetReviewDTO>> GetReviewByIdAsync(Guid reviewId);

        /// <summary>
        /// Get all reviews for a shop (paginated)
        /// </summary>
        Task<BaseResponse<IEnumerable<GetReviewDTO>>> GetShopReviewsAsync(
            Guid shopId,
            int pageNumber = 1,
            int pageSize = 10);

        /// <summary>
        /// Get all reviews for a product (paginated)
        /// </summary>
        Task<BaseResponse<IEnumerable<GetReviewDTO>>> GetProductReviewsAsync(
            Guid productId,
            int pageNumber = 1,
            int pageSize = 10);

        /// <summary>
        /// Get review summary for a shop (ratings breakdown)
        /// </summary>
        Task<BaseResponse<ShopReviewsSummaryDTO>> GetShopReviewsSummaryAsync(Guid shopId);

        /// <summary>
        /// Add seller's response to a review
        /// </summary>
        Task<BaseResponse<GetReviewDTO>> AddSellerResponseAsync(ReviewResponseDTO responseDTO, string vendorId);

        /// <summary>
        /// Mark review as helpful (upvote)
        /// </summary>
        Task<BaseResponse<int>> MarkReviewAsHelpfulAsync(Guid reviewId);

        /// <summary>
        /// Delete a review (admin or author only)
        /// </summary>
        Task<BaseResponse<string>> DeleteReviewAsync(Guid reviewId, string userId);

        /// <summary>
        /// Get average rating for a shop
        /// </summary>
        Task<BaseResponse<decimal>> GetShopAverageRatingAsync(Guid shopId);

        /// <summary>
        /// Get reviews by a specific reviewer
        /// </summary>
        Task<BaseResponse<IEnumerable<GetReviewDTO>>> GetReviewerHistoryAsync(
            string reviewerId,
            int pageNumber = 1,
            int pageSize = 10);
    }
}
