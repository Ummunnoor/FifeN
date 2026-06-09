using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Modules.TrustSafety.DTOs;
using Application.Modules.TrustSafety.Services.Interfaces;
using Domain.Entities.Enums;
using Domain.Entities.TrustSafety;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Modules.TrustSafety
{
    /// <summary>EF Core data access for reports, including the per-vendor open-report count behind auto-flagging.</summary>
    public sealed class ReportRepository(FifeNDbContext db) : IReportRepository
    {
        public async Task<Guid?> ResolveTargetVendorAsync(
            ReportTargetType targetType, Guid targetId, CancellationToken ct) => targetType switch
        {
            ReportTargetType.Vendor => await db.VendorProfiles.AsNoTracking()
                .Where(v => v.Id == targetId).Select(v => (Guid?)v.Id).FirstOrDefaultAsync(ct),

            ReportTargetType.Listing => await db.Products.AsNoTracking()
                .Where(p => p.Id == targetId).Select(p => (Guid?)p.VendorProfileId).FirstOrDefaultAsync(ct),

            ReportTargetType.Review => await db.Reviews.IgnoreQueryFilters().AsNoTracking()
                .Where(r => r.Id == targetId).Select(r => (Guid?)r.VendorProfileId).FirstOrDefaultAsync(ct),

            _ => null
        };

        public async Task AddAsync(Report report, CancellationToken ct)
        {
            db.Reports.Add(report);
            await db.SaveChangesAsync(ct);
        }

        public Task<int> CountOpenAgainstVendorAsync(Guid vendorProfileId, CancellationToken ct)
        {
            var productIds = db.Products.Where(p => p.VendorProfileId == vendorProfileId).Select(p => p.Id);
            var reviewIds = db.Reviews.IgnoreQueryFilters()
                .Where(r => r.VendorProfileId == vendorProfileId).Select(r => r.Id);

            return db.Reports.CountAsync(r => r.Status == ReportStatus.Open && (
                (r.TargetType == ReportTargetType.Vendor && r.TargetId == vendorProfileId) ||
                (r.TargetType == ReportTargetType.Listing && productIds.Contains(r.TargetId)) ||
                (r.TargetType == ReportTargetType.Review && reviewIds.Contains(r.TargetId))), ct);
        }

        public Task<Report?> GetTrackedAsync(Guid id, CancellationToken ct) =>
            db.Reports.FirstOrDefaultAsync(r => r.Id == id, ct);

        public async Task SaveAsync(Report report, CancellationToken ct)
        {
            db.Reports.Update(report);
            await db.SaveChangesAsync(ct);
        }

        public async Task<PagedResponse<ReportResponse>> GetQueueAsync(
            ReportStatus? status, int page, int pageSize, CancellationToken ct)
        {
            var query = db.Reports.AsNoTracking().AsQueryable();
            if (status is { } s)
                query = query.Where(r => r.Status == s);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(r => r.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReportResponse(
                    r.Id, r.TargetType, r.TargetId, r.Reason, r.Note, r.Status,
                    r.ResolvedByUserId, r.ResolvedAtUtc, r.CreatedAtUtc))
                .ToListAsync(ct);

            return new PagedResponse<ReportResponse>(items, page, pageSize, total);
        }
    }
}
