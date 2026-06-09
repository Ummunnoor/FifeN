using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.DTOs;
using Application.Exceptions;
using Application.Modules.Engagement.DTOs;
using Application.Modules.Engagement.Services.Interfaces;
using Application.Modules.Vendors.Services.Interfaces;
using Domain.Entities.Enums;
using Domain.Entities.Reviews;

namespace Application.Modules.Engagement.Services.Implementations
{
    /// <summary>
    /// Interaction-gated reviews. A buyer may review a listing only after contacting the vendor about it
    /// and waiting out a cooldown, and only once per listing. Reviews are editable for a limited window.
    /// Every review is honestly badged "Reviewer contacted this vendor" — never "verified purchase".
    /// </summary>
    public class ReviewService(
        IReviewRepository reviews,
        IInteractionRepository interactions,
        IVendorRepository vendors,
        IUserAdminStore users,
        INotificationService notifications) : IReviewService
    {
        private static readonly TimeSpan EligibilityCooldown = TimeSpan.FromHours(48);
        private static readonly TimeSpan EditWindow = TimeSpan.FromDays(30);
        private const int MaxPageSize = 50;

        public async Task<ReviewResponse> CreateAsync(
            Guid authorUserId, Guid productId, CreateReviewRequest request, CancellationToken ct)
        {
            // Eligibility: the buyer must have a lead on this listing that is past the cooldown.
            var gate = await interactions.GetByBuyerAndProductAsync(authorUserId, productId, ct);
            if (gate is null)
                throw new ForbiddenException("You can only review a listing after contacting the vendor about it.");
            if (DateTimeOffset.UtcNow - gate.CreatedAtUtc < EligibilityCooldown)
                throw new ForbiddenException("You can post a review only 48 hours after contacting the vendor.");

            if (await reviews.ExistsByAuthorAndProductAsync(authorUserId, productId, ct))
                throw new ConflictException("You have already reviewed this listing.");

            var review = new Review
            {
                Id = Guid.CreateVersion7(),
                AuthorUserId = authorUserId,
                ProductId = productId,
                VendorProfileId = gate.VendorProfileId,
                InteractionId = gate.Id,
                Rating = request.Rating,
                Text = string.IsNullOrWhiteSpace(request.Text) ? null : request.Text.Trim(),
                Status = ReviewStatus.Visible,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            await reviews.AddAsync(review, ct);

            var vendor = await vendors.GetProfileAsync(gate.VendorProfileId, ct);
            if (vendor is not null)
                await notifications.NotifyAsync(vendor.UserId, NotificationType.NewReview, "New review",
                    $"You received a {request.Rating}-star review.", ct);

            return ToResponse(review, await AuthorName(authorUserId, ct));
        }

        public async Task<ReviewResponse> UpdateAsync(
            Guid authorUserId, Guid reviewId, UpdateReviewRequest request, CancellationToken ct)
        {
            var review = await reviews.GetTrackedAsync(reviewId, ct)
                ?? throw new NotFoundException("Review not found.");
            if (review.AuthorUserId != authorUserId)
                throw new ForbiddenException("You can only edit your own review.");
            if (DateTimeOffset.UtcNow - review.CreatedAtUtc > EditWindow)
                throw new ForbiddenException("Reviews can no longer be edited after 30 days.");

            review.Rating = request.Rating;
            review.Text = string.IsNullOrWhiteSpace(request.Text) ? null : request.Text.Trim();
            review.EditedAtUtc = DateTimeOffset.UtcNow;
            await reviews.SaveAsync(review, ct);

            return ToResponse(review, await AuthorName(authorUserId, ct));
        }

        public Task<PagedResponse<ReviewResponse>> GetForProductAsync(
            Guid productId, int page, int pageSize, CancellationToken ct) =>
            reviews.GetForProductAsync(productId, Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize), ct);

        private async Task<string> AuthorName(Guid userId, CancellationToken ct) =>
            await users.GetDisplayNameAsync(userId, ct) ?? "A buyer";

        // Every review reaching persistence is interaction-gated, so the badge is always true.
        private static ReviewResponse ToResponse(Review r, string authorName) =>
            new(r.Id, authorName, r.Rating, r.Text, ContactedVendor: true, r.CreatedAtUtc);
    }
}
