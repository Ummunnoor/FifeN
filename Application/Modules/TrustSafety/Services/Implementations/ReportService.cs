using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.Exceptions;
using Application.Modules.TrustSafety.DTOs;
using Application.Modules.TrustSafety.Services.Interfaces;
using Domain.Entities.Enums;
using Domain.Entities.TrustSafety;

namespace Application.Modules.TrustSafety.Services.Implementations
{
    /// <summary>
    /// Records buyer reports against listings, vendors, and reviews. Every report names a target that
    /// must exist; the moderation queue and resolution live on the admin service. Once the open reports
    /// attributable to a vendor cross a threshold, the vendor is auto-flagged for admin attention.
    /// </summary>
    public class ReportService(IReportRepository reports, IAuditLogger audit) : IReportService
    {
        /// <summary>Open reports against a vendor that trip an automatic flag for admin review.</summary>
        private const int AutoFlagThreshold = 3;

        /// <summary>Audit actor for system-generated (non-admin) records.</summary>
        private static readonly Guid SystemActor = Guid.Empty;

        public async Task<ReportResponse> CreateAsync(
            Guid reporterUserId, CreateReportRequest request, CancellationToken ct)
        {
            // "Target must exist" — resolving the owning vendor also proves existence.
            var vendorProfileId = await reports.ResolveTargetVendorAsync(request.TargetType, request.TargetId, ct)
                ?? throw new NotFoundException("The reported item does not exist.");

            var report = new Report
            {
                Id = Guid.CreateVersion7(),
                ReporterUserId = reporterUserId,
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                Reason = request.Reason,
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                Status = ReportStatus.Open,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            await reports.AddAsync(report, ct);

            // Auto-flag once, when this report is the one that reaches the threshold.
            var openReports = await reports.CountOpenAgainstVendorAsync(vendorProfileId, ct);
            if (openReports == AutoFlagThreshold)
                await audit.WriteAsync(SystemActor, "Vendor.AutoFlagged", "VendorProfile", vendorProfileId,
                    reason: $"Open reports reached the auto-flag threshold ({AutoFlagThreshold}).",
                    metadataJson: $"{{\"openReports\":{openReports}}}", ipAddress: null, ct: ct);

            return ToResponse(report);
        }

        private static ReportResponse ToResponse(Report r) =>
            new(r.Id, r.TargetType, r.TargetId, r.Reason, r.Note, r.Status,
                r.ResolvedByUserId, r.ResolvedAtUtc, r.CreatedAtUtc);
    }
}
