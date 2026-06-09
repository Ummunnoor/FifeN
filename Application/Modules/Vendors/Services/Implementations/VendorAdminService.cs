using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.Exceptions;
using Application.Modules.Vendors.DTOs;
using Application.Modules.Vendors.Services.Interfaces;
using Domain.Entities.Enums;
using Domain.Entities.Vendors;
using Microsoft.Extensions.Logging;

namespace Application.Modules.Vendors.Services.Implementations
{
    /// <summary>Admin vendor moderation: queue, approve/reject applications, suspend/reinstate vendors.</summary>
    public class VendorAdminService(
        IVendorRepository vendors,
        IUserAdminStore users,
        IAuditLogger audit,
        INotificationService notifications,
        ILogger<VendorAdminService> logger) : IVendorAdminService
    {
        public async Task<IReadOnlyList<VendorRequestQueueItem>> GetQueueAsync(
            VendorRequestStatus status, int page, int pageSize, CancellationToken ct)
        {
            var requests = await vendors.GetRequestsByStatusAsync(status, page, pageSize, ct);
            return requests.Select(r => new VendorRequestQueueItem(
                r.Id, r.UserId, r.BusinessName, r.VerificationMethod,
                r.VerificationStatus, r.NameMatch, r.CreatedAtUtc)).ToList();
        }

        public async Task ApproveAsync(Guid adminUserId, Guid requestId, string? ipAddress, CancellationToken ct)
        {
            var request = await RequirePendingRequest(requestId, ct);
            var now = DateTimeOffset.UtcNow;

            request.Status = VendorRequestStatus.Approved;
            request.ReviewedByUserId = adminUserId;
            request.ReviewedAtUtc = now;

            var profile = new VendorProfile
            {
                Id = Guid.CreateVersion7(),
                UserId = request.UserId,
                BusinessName = request.BusinessName,
                WhatsAppNumber = request.WhatsAppNumber,
                VerificationMethod = request.VerificationMethod,
                VerificationStatus = VerificationStatus.Verified,
                NameMatch = request.NameMatch,
                KycReference = request.KycReference,
                TrustTier = TrustTier.Probation,
                CreatedAtUtc = now,
                ApprovedAtUtc = now
            };

            await vendors.SaveRequestAsync(request, ct);
            await vendors.AddProfileAsync(profile, ct);
            await users.GrantVendorAsync(request.UserId, ct);

            await audit.WriteAsync(adminUserId, "VendorRequest.Approve", nameof(VendorRequest), request.Id,
                ipAddress: ipAddress, ct: ct);
            await notifications.NotifyAsync(request.UserId, NotificationType.VendorApproved,
                "You're now a verified vendor", "Your vendor application was approved. You can start listing products.", ct);

            logger.LogInformation("Vendor request {RequestId} approved by {AdminId}.", requestId, adminUserId);
        }

        public async Task RejectAsync(
            Guid adminUserId, Guid requestId, RejectionReason reason, string? ipAddress, CancellationToken ct)
        {
            var request = await RequirePendingRequest(requestId, ct);

            request.Status = VendorRequestStatus.Rejected;
            request.RejectionReason = reason;
            request.ReviewedByUserId = adminUserId;
            request.ReviewedAtUtc = DateTimeOffset.UtcNow;

            await vendors.SaveRequestAsync(request, ct);
            await audit.WriteAsync(adminUserId, "VendorRequest.Reject", nameof(VendorRequest), request.Id,
                reason: reason.ToString(), ipAddress: ipAddress, ct: ct);
            await notifications.NotifyAsync(request.UserId, NotificationType.VendorRejected,
                "Vendor application update", $"Your vendor application was not approved ({reason}). You may fix the issue and re-apply.", ct);
        }

        public async Task SuspendAsync(
            Guid adminUserId, Guid vendorProfileId, string reason, string? ipAddress, CancellationToken ct)
        {
            var profile = await vendors.GetProfileAsync(vendorProfileId, ct)
                ?? throw new NotFoundException("Vendor not found.");

            await users.SetStatusAsync(profile.UserId, UserStatus.Suspended, ct);
            await audit.WriteAsync(adminUserId, "Vendor.Suspend", nameof(VendorProfile), profile.Id,
                reason: reason, ipAddress: ipAddress, ct: ct);
            await notifications.NotifyAsync(profile.UserId, NotificationType.Security,
                "Account suspended", $"Your vendor account has been suspended. Reason: {reason}.", ct);
        }

        public async Task ReinstateAsync(Guid adminUserId, Guid vendorProfileId, string? ipAddress, CancellationToken ct)
        {
            var profile = await vendors.GetProfileAsync(vendorProfileId, ct)
                ?? throw new NotFoundException("Vendor not found.");

            await users.SetStatusAsync(profile.UserId, UserStatus.Active, ct);
            await audit.WriteAsync(adminUserId, "Vendor.Reinstate", nameof(VendorProfile), profile.Id,
                ipAddress: ipAddress, ct: ct);
            await notifications.NotifyAsync(profile.UserId, NotificationType.Security,
                "Account reinstated", "Your vendor account has been reinstated and your listings are visible again.", ct);
        }

        private async Task<VendorRequest> RequirePendingRequest(Guid requestId, CancellationToken ct)
        {
            var request = await vendors.GetRequestAsync(requestId, ct)
                ?? throw new NotFoundException("Vendor request not found.");
            if (request.Status != VendorRequestStatus.Pending)
                throw new ConflictException("This request has already been actioned.");
            return request;
        }
    }
}
