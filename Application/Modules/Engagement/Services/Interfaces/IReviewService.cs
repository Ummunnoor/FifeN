using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Modules.Engagement.DTOs;
using Domain.Entities.Reviews;

namespace Application.Modules.Engagement.Services.Interfaces
{
    /// <summary>Interaction-gated buyer reviews and public review listing.</summary>
    public interface IReviewService
    {
        Task<ReviewResponse> CreateAsync(
            Guid authorUserId, Guid productId, CreateReviewRequest request, CancellationToken ct);

        Task<ReviewResponse> UpdateAsync(
            Guid authorUserId, Guid reviewId, UpdateReviewRequest request, CancellationToken ct);

        Task<PagedResponse<ReviewResponse>> GetForProductAsync(
            Guid productId, int page, int pageSize, CancellationToken ct);
    }

    /// <summary>Admin review moderation.</summary>
    public interface IReviewAdminService
    {
        Task ModerateAsync(
            Guid adminUserId, Guid reviewId, ModerateReviewRequest request, string? ipAddress, CancellationToken ct);
    }

    /// <summary>Data access for reviews.</summary>
    public interface IReviewRepository
    {
        /// <summary>True when the author already has any review (visible or not) for the product.</summary>
        Task<bool> ExistsByAuthorAndProductAsync(Guid authorUserId, Guid productId, CancellationToken ct);

        Task AddAsync(Review review, CancellationToken ct);

        /// <summary>Loads a visible review for author editing (tracked).</summary>
        Task<Review?> GetTrackedAsync(Guid id, CancellationToken ct);

        /// <summary>Loads any review regardless of visibility, for admin moderation (tracked).</summary>
        Task<Review?> GetForModerationAsync(Guid id, CancellationToken ct);

        Task SaveAsync(Review review, CancellationToken ct);

        /// <summary>A product's visible reviews, newest first, with author display names.</summary>
        Task<PagedResponse<ReviewResponse>> GetForProductAsync(
            Guid productId, int page, int pageSize, CancellationToken ct);
    }
}
