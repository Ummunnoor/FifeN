using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Enums;
using Domain.Entities.Vendors;

namespace Application.Modules.Vendors.Services.Interfaces
{
    /// <summary>Data access for vendor requests and profiles.</summary>
    public interface IVendorRepository
    {
        // Requests
        Task<bool> BusinessNameExistsAsync(string businessName, CancellationToken ct);
        Task<bool> HasPendingRequestAsync(Guid userId, CancellationToken ct);
        Task AddRequestAsync(VendorRequest request, CancellationToken ct);
        Task<VendorRequest?> GetLatestRequestAsync(Guid userId, CancellationToken ct);
        Task<VendorRequest?> GetRequestAsync(Guid requestId, CancellationToken ct);
        Task<IReadOnlyList<VendorRequest>> GetRequestsByStatusAsync(
            VendorRequestStatus status, int page, int pageSize, CancellationToken ct);
        Task SaveRequestAsync(VendorRequest request, CancellationToken ct);

        // Profiles
        Task AddProfileAsync(VendorProfile profile, CancellationToken ct);
        Task<VendorProfile?> GetProfileByUserAsync(Guid userId, CancellationToken ct);
        Task<VendorProfile?> GetProfileAsync(Guid vendorProfileId, CancellationToken ct);
        Task SaveProfileAsync(VendorProfile profile, CancellationToken ct);

        /// <summary>Average rating and review count for a vendor's visible reviews.</summary>
        Task<(double Average, int Count)> GetRatingAsync(Guid vendorProfileId, CancellationToken ct);
    }

    /// <summary>Account mutations that require ASP.NET Identity (role/flag/status changes).</summary>
    public interface IUserAdminStore
    {
        Task GrantVendorAsync(Guid userId, CancellationToken ct);
        Task SetStatusAsync(Guid userId, UserStatus status, CancellationToken ct);
        Task<IReadOnlyDictionary<Guid, UserStatus>> GetStatusesAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct);
        Task<bool> IsVendorAsync(Guid userId, CancellationToken ct);
        Task<string?> GetDisplayNameAsync(Guid userId, CancellationToken ct);
    }
}
