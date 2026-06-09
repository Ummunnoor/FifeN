using System;
using Domain.Entities.Enums;

namespace Domain.Entities.TrustSafety
{
    /// <summary>
    /// A buyer-submitted report against a listing, vendor, or review. Report counts per vendor are
    /// tracked; crossing a threshold auto-flags the vendor for admin review.
    /// </summary>
    public class Report
    {
        public Guid Id { get; set; }

        /// <summary>The user who filed the report.</summary>
        public Guid ReporterUserId { get; set; }

        /// <summary>The kind of object being reported.</summary>
        public ReportTargetType TargetType { get; set; }

        /// <summary>Id of the reported object (polymorphic; interpreted via <see cref="TargetType"/>).</summary>
        public Guid TargetId { get; set; }

        /// <summary>Why the object was reported.</summary>
        public ReportReason Reason { get; set; }

        /// <summary>Optional free-text detail from the reporter.</summary>
        public string? Note { get; set; }

        /// <summary>Lifecycle state in the moderation queue.</summary>
        public ReportStatus Status { get; set; } = ReportStatus.Open;

        /// <summary>Admin who resolved the report.</summary>
        public Guid? ResolvedByUserId { get; set; }

        /// <summary>When the report was resolved (UTC).</summary>
        public DateTimeOffset? ResolvedAtUtc { get; set; }

        /// <summary>When the report was filed (UTC).</summary>
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
