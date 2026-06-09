using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.Exceptions;
using Application.Modules.Engagement.DTOs;
using Application.Modules.Engagement.Services.Interfaces;
using Domain.Entities.Reviews;
using Microsoft.Extensions.Logging;

namespace Application.Modules.Engagement.Services.Implementations
{
    /// <summary>Admin review moderation: hide, remove, or restore a review. All actions are audited.</summary>
    public class ReviewAdminService(
        IReviewRepository reviews,
        IAuditLogger audit,
        ILogger<ReviewAdminService> logger) : IReviewAdminService
    {
        public async Task ModerateAsync(
            Guid adminUserId, Guid reviewId, ModerateReviewRequest request, string? ipAddress, CancellationToken ct)
        {
            var review = await reviews.GetForModerationAsync(reviewId, ct)
                ?? throw new NotFoundException("Review not found.");

            review.Status = request.Status;
            await reviews.SaveAsync(review, ct);

            await audit.WriteAsync(adminUserId, "Review.Moderate", nameof(Review), review.Id,
                reason: request.Reason,
                metadataJson: $"{{\"status\":\"{request.Status}\"}}",
                ipAddress: ipAddress, ct: ct);

            logger.LogInformation("Review {ReviewId} moderated to {Status} by {AdminId}.",
                reviewId, request.Status, adminUserId);
        }
    }
}
