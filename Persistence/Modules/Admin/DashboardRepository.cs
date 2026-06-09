using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Modules.Admin.DTOs;
using Application.Modules.Admin.Services.Interfaces;
using Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Modules.Admin
{
    /// <summary>
    /// Aggregated read queries for the admin dashboard. "Active" follows BRD §9 (30-day rolling
    /// windows): an active listing is Live and updated within 30 days; an active user has been seen
    /// within 30 days. Cross-discovery rate — buyers contacting a vendor they had not engaged before —
    /// is the north-star headline.
    /// </summary>
    public sealed class DashboardRepository(FifeNDbContext db) : IDashboardRepository
    {
        private const int TopN = 5;

        public async Task<DashboardResponse> GetDashboardAsync(CancellationToken ct)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-30);

            var registeredUsers = await db.Users.CountAsync(ct);
            var activeUsers30d = await db.Users
                .CountAsync(u => u.LastActiveAtUtc != null && u.LastActiveAtUtc >= cutoff, ct);

            var activeListings = await db.Products
                .CountAsync(p => p.Status == ListingStatus.Live && p.UpdatedAtUtc >= cutoff, ct);

            var otpTotal = await db.PhoneVerifications.CountAsync(v => v.CreatedAtUtc >= cutoff, ct);
            var otpVerified = await db.PhoneVerifications
                .CountAsync(v => v.CreatedAtUtc >= cutoff && v.Status == OtpStatus.Verified, ct);
            var otpSuccessRate = otpTotal == 0 ? 0d : Math.Round((double)otpVerified / otpTotal, 4);

            var interactions30d = await db.Interactions.CountAsync(i => i.CreatedAtUtc >= cutoff, ct);
            var crossDiscovery30d = await db.Interactions
                .CountAsync(i => i.CreatedAtUtc >= cutoff && i.IsCrossDiscovery, ct);
            var crossDiscoveryRate = interactions30d == 0
                ? 0d : Math.Round((double)crossDiscovery30d / interactions30d, 4);

            var topCategoriesRaw = await db.Products
                .Where(p => p.Status == ListingStatus.Live)
                .GroupBy(p => p.Category.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(TopN)
                .ToListAsync(ct);
            var topCategories = topCategoriesRaw.Select(x => new NameCount(x.Name, x.Count)).ToList();

            var topLocationsRaw = await db.Products
                .Where(p => p.Status == ListingStatus.Live)
                .GroupBy(p => p.Location.State)
                .Select(g => new { State = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(TopN)
                .ToListAsync(ct);
            var topLocations = topLocationsRaw.Select(x => new NameCount(x.State.ToString(), x.Count)).ToList();

            var pendingApprovals = await db.VendorRequests
                .CountAsync(r => r.Status == VendorRequestStatus.Pending, ct);
            var openReports = await db.Reports.CountAsync(r => r.Status == ReportStatus.Open, ct);

            return new DashboardResponse(
                registeredUsers, activeUsers30d, activeListings, otpSuccessRate,
                interactions30d, crossDiscoveryRate, topCategories, topLocations,
                pendingApprovals, openReports);
        }
    }
}
