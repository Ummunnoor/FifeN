using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Exceptions;
using Application.Modules.Vendors.Services.Interfaces;
using Domain.Entities.Enums;
using Domain.Entities.Vendors;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Modules.Vendors
{
    /// <summary>EF Core data access for vendor requests and profiles.</summary>
    public sealed class VendorRepository(FifeNDbContext db) : IVendorRepository
    {
        public Task<bool> BusinessNameExistsAsync(string businessName, CancellationToken ct) =>
            db.VendorProfiles.AnyAsync(p => p.BusinessName == businessName, ct);

        public Task<bool> HasPendingRequestAsync(Guid userId, CancellationToken ct) =>
            db.VendorRequests.AnyAsync(
                r => r.UserId == userId && r.Status == VendorRequestStatus.Pending, ct);

        public async Task AddRequestAsync(VendorRequest request, CancellationToken ct)
        {
            db.VendorRequests.Add(request);
            await db.SaveChangesAsync(ct);
        }

        public Task<VendorRequest?> GetLatestRequestAsync(Guid userId, CancellationToken ct) =>
            db.VendorRequests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);

        public Task<VendorRequest?> GetRequestAsync(Guid requestId, CancellationToken ct) =>
            db.VendorRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);

        public async Task<IReadOnlyList<VendorRequest>> GetRequestsByStatusAsync(
            VendorRequestStatus status, int page, int pageSize, CancellationToken ct)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            return await db.VendorRequests
                .Where(r => r.Status == status)
                .OrderBy(r => r.CreatedAtUtc)
                .Skip((Math.Max(page, 1) - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
        }

        public async Task SaveRequestAsync(VendorRequest request, CancellationToken ct)
        {
            db.VendorRequests.Update(request);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("This request was modified by another admin. Refresh and try again.");
            }
        }

        public async Task AddProfileAsync(VendorProfile profile, CancellationToken ct)
        {
            db.VendorProfiles.Add(profile);
            await db.SaveChangesAsync(ct);
        }

        public Task<VendorProfile?> GetProfileByUserAsync(Guid userId, CancellationToken ct) =>
            db.VendorProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);

        public Task<VendorProfile?> GetProfileAsync(Guid vendorProfileId, CancellationToken ct) =>
            db.VendorProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == vendorProfileId, ct);

        public async Task SaveProfileAsync(VendorProfile profile, CancellationToken ct)
        {
            db.VendorProfiles.Update(profile);
            await db.SaveChangesAsync(ct);
        }

        public async Task<(double Average, int Count)> GetRatingAsync(Guid vendorProfileId, CancellationToken ct)
        {
            // The Review query filter restricts this to visible reviews only.
            var ratings = await db.Reviews
                .Where(r => r.VendorProfileId == vendorProfileId)
                .Select(r => r.Rating)
                .ToListAsync(ct);

            return ratings.Count == 0 ? (0d, 0) : (Math.Round(ratings.Average(), 2), ratings.Count);
        }
    }
}
