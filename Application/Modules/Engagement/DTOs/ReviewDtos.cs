using System;
using Domain.Entities.Enums;

namespace Application.Modules.Engagement.DTOs
{
    /// <summary>Buyer's new-review payload.</summary>
    public record CreateReviewRequest(int Rating, string? Text);

    /// <summary>Buyer's edit to their existing review.</summary>
    public record UpdateReviewRequest(int Rating, string? Text);

    /// <summary>A review as shown publicly. <see cref="ContactedVendor"/> drives the honest badge.</summary>
    public record ReviewResponse(
        Guid Id,
        string AuthorDisplayName,
        int Rating,
        string? Text,
        bool ContactedVendor,
        DateTimeOffset CreatedAtUtc);

    /// <summary>Admin moderation of a review (hide/remove/restore); reason is recorded in the audit log.</summary>
    public record ModerateReviewRequest(ReviewStatus Status, string? Reason);
}
