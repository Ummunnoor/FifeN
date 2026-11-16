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

            var userStats = await db.Users
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Registered = g.Count(),
                    Active = g.Count(u => u.LastActiveAtUtc != null && u.LastActiveAtUtc >= cutoff)
                })
                .FirstOrDefaultAsync(ct);
            var registeredUsers = userStats?.Registered ?? 0;
            var activeUsers30d = userStats?.Active ?? 0;

            var activeListings = await db.Products
                .CountAsync(p => p.Status == ListingStatus.Live && p.UpdatedAtUtc >= cutoff, ct);

            var otpStats = await db.EmailVerifications
                .Where(v => v.CreatedAtUtc >= cutoff)
                .GroupBy(_ => 1)
                .Select(g => new { Total = g.Count(), Verified = g.Count(v => v.IsConsumed) })
                .FirstOrDefaultAsync(ct);
            var otpTotal = otpStats?.Total ?? 0;
            var otpVerified = otpStats?.Verified ?? 0;
            var otpSuccessRate = otpTotal == 0 ? 0d : Math.Round((double)otpVerified / otpTotal, 4);

            var interactionStats = await db.Interactions
                .Where(i => i.CreatedAtUtc >= cutoff)
                .GroupBy(_ => 1)
                .Select(g => new { Total = g.Count(), CrossDiscovery = g.Count(i => i.IsCrossDiscovery) })
                .FirstOrDefaultAsync(ct);
            var interactions30d = interactionStats?.Total ?? 0;
            var crossDiscovery30d = interactionStats?.CrossDiscovery ?? 0;
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
