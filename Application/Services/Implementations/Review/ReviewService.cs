using Application.DTOs;
using Application.DTOs.Review;
using Application.Services.Interfaces;
using Application.Services.Interfaces.Logging;
using Domain.Entities.Product;

namespace Application.Services.Implementations.Review
{
    /// <summary>
    /// Implementation of review and rating management service
    /// </summary>
    public class ReviewService : IReviewService
    {
        private readonly IGeneric<Domain.Entities.Product.Review> _reviewRepository;
        private readonly IAppLogger<ReviewService> _logger;

        public ReviewService(
            IGeneric<Domain.Entities.Product.Review> reviewRepository,
            IAppLogger<ReviewService> logger)
        {
            _reviewRepository = reviewRepository;
            _logger = logger;
        }

        public async Task<BaseResponse<GetReviewDTO>> CreateReviewAsync(CreateReviewDTO createReviewDTO, string reviewerId)
        {
            try
            {
                // TODO: Verify purchase (order must be completed)
                var newReview = new Domain.Entities.Product.Review
                {
                    ShopId = createReviewDTO.ShopId,
                    ProductId = createReviewDTO.ProductId,
                    OrderId = createReviewDTO.OrderId,
                    ReviewerId = reviewerId,
                    Rating = createReviewDTO.Rating,
                    Title = createReviewDTO.Title,
                    Comment = createReviewDTO.Comment,
                    IsVerifiedPurchase = createReviewDTO.OrderId.HasValue,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _reviewRepository.AddAsync(newReview);
                _logger.LogInformation($"Review created: {newReview.Id}");

                return new BaseResponse<GetReviewDTO>(
                    true,
                    "Review created successfully",
                    MapToGetReviewDTO(newReview));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error creating review", ex);
                return new BaseResponse<GetReviewDTO>(false, "An error occurred while creating review");
            }
        }

        public async Task<BaseResponse<GetReviewDTO>> GetReviewByIdAsync(Guid reviewId)
        {
            try
            {
                var review = await _reviewRepository.GetByIdAsync(reviewId);
                if (review == null)
                    return new BaseResponse<GetReviewDTO>(false, "Review not found");

                return new BaseResponse<GetReviewDTO>(
                    true,
                    "Review retrieved successfully",
                    MapToGetReviewDTO(review));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting review", ex);
                return new BaseResponse<GetReviewDTO>(false, "An error occurred");
            }
        }

        public async Task<BaseResponse<IEnumerable<GetReviewDTO>>> GetShopReviewsAsync(
            Guid shopId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var reviews = await _reviewRepository.GetAllAsync();
                var shopReviews = reviews
                    .Where(r => r.ShopId == shopId && r.IsActive)
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(MapToGetReviewDTO)
                    .ToList();

                return new BaseResponse<IEnumerable<GetReviewDTO>>(
                    true,
                    $"Retrieved {shopReviews.Count} reviews",
                    shopReviews);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting shop reviews", ex);
                return new BaseResponse<IEnumerable<GetReviewDTO>>(false, "An error occurred", new List<GetReviewDTO>());
            }
        }

        public async Task<BaseResponse<IEnumerable<GetReviewDTO>>> GetProductReviewsAsync(
            Guid productId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var reviews = await _reviewRepository.GetAllAsync();
                var productReviews = reviews
                    .Where(r => r.ProductId == productId && r.IsActive)
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(MapToGetReviewDTO)
                    .ToList();

                return new BaseResponse<IEnumerable<GetReviewDTO>>(
                    true,
                    $"Retrieved {productReviews.Count} reviews",
                    productReviews);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting product reviews", ex);
                return new BaseResponse<IEnumerable<GetReviewDTO>>(false, "An error occurred", new List<GetReviewDTO>());
            }
        }

        public async Task<BaseResponse<ShopReviewsSummaryDTO>> GetShopReviewsSummaryAsync(Guid shopId)
        {
            try
            {
                var reviews = await _reviewRepository.GetAllAsync();
                var shopReviews = reviews
                    .Where(r => r.ShopId == shopId && r.IsActive)
                    .ToList();

                var summary = new ShopReviewsSummaryDTO
                {
                    ShopId = shopId,
                    AverageRating = shopReviews.Any() ? (decimal)shopReviews.Average(r => r.Rating) : 0,
                    TotalReviews = shopReviews.Count,
                    RatingDistribution = Enumerable.Range(1, 5)
                        .ToDictionary(i => i, i => shopReviews.Count(r => r.Rating == i)),
                    RecentReviews = shopReviews
                        .OrderByDescending(r => r.CreatedAt)
                        .Take(5)
                        .Select(MapToGetReviewDTO)
                        .ToList()
                };

                return new BaseResponse<ShopReviewsSummaryDTO>(
                    true,
                    "Summary retrieved successfully",
                    summary);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting review summary", ex);
                return new BaseResponse<ShopReviewsSummaryDTO>(false, "An error occurred");
            }
        }

        public async Task<BaseResponse<GetReviewDTO>> AddSellerResponseAsync(ReviewResponseDTO responseDTO, string vendorId)
        {
            try
            {
                var review = await _reviewRepository.GetByIdAsync(responseDTO.ReviewId);
                if (review == null)
                    return new BaseResponse<GetReviewDTO>(false, "Review not found");

                // TODO: Verify vendorId owns the shop
                review.SellerResponse = responseDTO.SellerResponse;
                review.HasSellerResponse = true;
                review.UpdatedAt = DateTime.UtcNow;

                await _reviewRepository.UpdateAsync(review);
                return new BaseResponse<GetReviewDTO>(
                    true,
                    "Response added successfully",
                    MapToGetReviewDTO(review));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error adding seller response", ex);
                return new BaseResponse<GetReviewDTO>(false, "An error occurred");
            }
        }

        public async Task<BaseResponse<int>> MarkReviewAsHelpfulAsync(Guid reviewId)
        {
            try
            {
                var review = await _reviewRepository.GetByIdAsync(reviewId);
                if (review == null)
                    return new BaseResponse<int>(false, "Review not found");

                review.HelpfulCount++;
                await _reviewRepository.UpdateAsync(review);

                return new BaseResponse<int>(true, "Marked as helpful", review.HelpfulCount);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error marking review", ex);
                return new BaseResponse<int>(false, "An error occurred");
            }
        }

        public async Task<BaseResponse<string>> DeleteReviewAsync(Guid reviewId, string userId)
        {
            try
            {
                var review = await _reviewRepository.GetByIdAsync(reviewId);
                if (review == null)
                    return new BaseResponse<string>(false, "Review not found");

                // TODO: Verify userId is author or admin
                review.IsActive = false;
                review.UpdatedAt = DateTime.UtcNow;

                await _reviewRepository.UpdateAsync(review);
                return new BaseResponse<string>(true, "Review deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error deleting review", ex);
                return new BaseResponse<string>(false, "An error occurred");
            }
        }

        public async Task<BaseResponse<decimal>> GetShopAverageRatingAsync(Guid shopId)
        {
            try
            {
                var reviews = await _reviewRepository.GetAllAsync();
                var shopReviews = reviews.Where(r => r.ShopId == shopId && r.IsActive).ToList();
                var average = shopReviews.Any() ? (decimal)shopReviews.Average(r => r.Rating) : 0;

                return new BaseResponse<decimal>(true, "Average rating retrieved", average);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting average rating", ex);
                return new BaseResponse<decimal>(false, "An error occurred");
            }
        }

        public async Task<BaseResponse<IEnumerable<GetReviewDTO>>> GetReviewerHistoryAsync(
            string reviewerId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var reviews = await _reviewRepository.GetAllAsync();
                var reviewerReviews = reviews
                    .Where(r => r.ReviewerId == reviewerId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(MapToGetReviewDTO)
                    .ToList();

                return new BaseResponse<IEnumerable<GetReviewDTO>>(
                    true,
                    $"Retrieved {reviewerReviews.Count} reviews",
                    reviewerReviews);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting reviewer history", ex);
                return new BaseResponse<IEnumerable<GetReviewDTO>>(false, "An error occurred", new List<GetReviewDTO>());
            }
        }

        // Helper
        private GetReviewDTO MapToGetReviewDTO(Domain.Entities.Product.Review review)
        {
            return new GetReviewDTO
            {
                Id = review.Id,
                ShopId = review.ShopId,
                ReviewerName = review.Reviewer?.FullName ?? "Anonymous",
                Rating = review.Rating,
                Title = review.Title,
                Comment = review.Comment,
                IsVerifiedPurchase = review.IsVerifiedPurchase,
                HelpfulCount = review.HelpfulCount,
                CreatedAt = review.CreatedAt,
                SellerResponse = review.SellerResponse,
                HasSellerResponse = review.HasSellerResponse
            };
        }
    }
}
