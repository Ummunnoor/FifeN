using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Modules.Vendors.DTOs;
using Domain.Entities.Enums;

namespace Application.Modules.Vendors.Services.Interfaces
{
    /// <summary>Vendor self-service and public shop-header reads.</summary>
    public interface IVendorService
    {
        Task<VendorRequestResponse> SubmitRequestAsync(Guid userId, CreateVendorRequestRequest request, CancellationToken ct);
        Task<VendorRequestResponse> GetMyLatestRequestAsync(Guid userId, CancellationToken ct);
        Task UpdateProfileAsync(Guid userId, UpdateVendorProfileRequest request, CancellationToken ct);
        Task<VendorPublicResponse> GetPublicAsync(Guid vendorProfileId, CancellationToken ct);
    }

    /// <summary>Admin vendor moderation. All mutations are audited and reversible where possible.</summary>
    public interface IVendorAdminService
    {
        Task<IReadOnlyList<VendorRequestQueueItem>> GetQueueAsync(
            VendorRequestStatus status, int page, int pageSize, CancellationToken ct);
        Task ApproveAsync(Guid adminUserId, Guid requestId, string? ipAddress, CancellationToken ct);
        Task RejectAsync(Guid adminUserId, Guid requestId, RejectionReason reason, string? ipAddress, CancellationToken ct);
        Task SuspendAsync(Guid adminUserId, Guid vendorProfileId, string reason, string? ipAddress, CancellationToken ct);
        Task ReinstateAsync(Guid adminUserId, Guid vendorProfileId, string? ipAddress, CancellationToken ct);
    }
}
