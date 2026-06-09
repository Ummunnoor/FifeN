using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Modules.TrustSafety.DTOs;
using Domain.Entities.Enums;
using Domain.Entities.TrustSafety;

namespace Application.Modules.TrustSafety.Services.Interfaces
{
    /// <summary>Buyer-facing reporting of listings, vendors, and reviews.</summary>
    public interface IReportService
    {
        Task<ReportResponse> CreateAsync(Guid reporterUserId, CreateReportRequest request, CancellationToken ct);
    }

    /// <summary>Admin moderation queue and report resolution.</summary>
    public interface IReportAdminService
    {
        Task<PagedResponse<ReportResponse>> GetQueueAsync(
            ReportStatus? status, int page, int pageSize, CancellationToken ct);

        Task ResolveAsync(
            Guid adminUserId, Guid reportId, ResolveReportRequest request, string? ipAddress, CancellationToken ct);
    }

    /// <summary>Data access for reports.</summary>
    public interface IReportRepository
    {
        /// <summary>The owning vendor of a report target, or null if the target does not exist.</summary>
        Task<Guid?> ResolveTargetVendorAsync(ReportTargetType targetType, Guid targetId, CancellationToken ct);

        Task AddAsync(Report report, CancellationToken ct);

        /// <summary>Count of open reports attributable to a vendor (its profile, listings, and reviews).</summary>
        Task<int> CountOpenAgainstVendorAsync(Guid vendorProfileId, CancellationToken ct);

        Task<Report?> GetTrackedAsync(Guid id, CancellationToken ct);

        Task SaveAsync(Report report, CancellationToken ct);

        Task<PagedResponse<ReportResponse>> GetQueueAsync(
            ReportStatus? status, int page, int pageSize, CancellationToken ct);
    }
}
