using System;
using Domain.Entities.Enums;

namespace Application.Modules.TrustSafety.DTOs
{
    /// <summary>A buyer's report against a listing, vendor, or review.</summary>
    public record CreateReportRequest(ReportTargetType TargetType, Guid TargetId, ReportReason Reason, string? Note);

    /// <summary>An admin's resolution of a report (Actioned or Dismissed); the note is recorded in the audit log.</summary>
    public record ResolveReportRequest(ReportStatus Status, string? Note);

    /// <summary>A report as shown in the admin moderation queue.</summary>
    public record ReportResponse(
        Guid Id,
        ReportTargetType TargetType,
        Guid TargetId,
        ReportReason Reason,
        string? Note,
        ReportStatus Status,
        Guid? ResolvedByUserId,
        DateTimeOffset? ResolvedAtUtc,
        DateTimeOffset CreatedAtUtc);
}
