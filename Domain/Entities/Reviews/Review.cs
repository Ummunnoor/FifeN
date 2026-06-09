using System;
using Domain.Entities.Enums;

namespace Domain.Entities.Reviews
{
    /// <summary>
    /// An interaction-gated review of a product. Only a buyer who previously contacted the vendor
    /// (after a cooldown) may post one, and at most once per product. Badged honestly as
    /// "Reviewer contacted this vendor" — never "Verified purchase".
    /// </summary>
    public class Review
    {
        public Guid Id { get; set; }

        /// <summary>The buyer who wrote the review.</summary>
        public Guid AuthorUserId { get; set; }

        /// <summary>The reviewed product.</summary>
        public Guid ProductId { get; set; }

        /// <summary>Denormalized owning vendor, for fast vendor-rating roll-ups.</summary>
        public Guid VendorProfileId { get; set; }

        /// <summary>The qualifying interaction that gates this review.</summary>
        public Guid InteractionId { get; set; }

        /// <summary>Star rating, 1–5.</summary>
        public int Rating { get; set; }

        /// <summary>Optional short free text.</summary>
        public string? Text { get; set; }

        /// <summary>Moderation visibility.</summary>
        public ReviewStatus Status { get; set; } = ReviewStatus.Visible;

        /// <summary>When the review was posted (UTC).</summary>
        public DateTimeOffset CreatedAtUtc { get; set; }

        /// <summary>When the review was last edited (UTC); editable within 30 days.</summary>
        public DateTimeOffset? EditedAtUtc { get; set; }
    }
}
