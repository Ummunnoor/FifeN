using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.DTOs;
using Application.Exceptions;
using Application.Modules.TrustSafety.DTOs;
using Application.Modules.TrustSafety.Services.Interfaces;
using Domain.Entities.Enums;
using Domain.Entities.TrustSafety;
using Microsoft.Extensions.Logging;

namespace Application.Modules.TrustSafety.Services.Implementations
{
    /// <summary>Admin moderation queue and report resolution. Resolutions are audited.</summary>
    public class ReportAdminService(
        IReportRepository reports,
        IAuditLogger audit,
        ILogger<ReportAdminService> logger) : IReportAdminService
    {
        private const int MaxPageSize = 50;

        public Task<PagedResponse<ReportResponse>> GetQueueAsync(
            ReportStatus? status, int page, int pageSize, CancellationToken ct) =>
            reports.GetQueueAsync(status, Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize), ct);

        public async Task ResolveAsync(
            Guid adminUserId, Guid reportId, ResolveReportRequest request, string? ipAddress, CancellationToken ct)
        {
            if (request.Status is not (ReportStatus.Actioned or ReportStatus.Dismissed))
                throw new BusinessRuleException("A report can only be resolved as Actioned or Dismissed.");

            var report = await reports.GetTrackedAsync(reportId, ct)
                ?? throw new NotFoundException("Report not found.");
            if (report.Status != ReportStatus.Open)
                throw new ConflictException("This report has already been resolved.");

            report.Status = request.Status;
            report.ResolvedByUserId = adminUserId;
            report.ResolvedAtUtc = DateTimeOffset.UtcNow;
            await reports.SaveAsync(report, ct);

            // The reporter's Note stays intact; the moderator's rationale is captured in the audit log.
            await audit.WriteAsync(adminUserId, "Report.Resolve", nameof(Report), report.Id,
                reason: request.Note,
                metadataJson: $"{{\"status\":\"{request.Status}\"}}",
                ipAddress: ipAddress, ct: ct);

            logger.LogInformation("Report {ReportId} resolved as {Status} by {AdminId}.",
                reportId, request.Status, adminUserId);
        }
    }
}
