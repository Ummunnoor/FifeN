using System;
using Domain.Entities.Enums;

namespace Application.Modules.Vendors.DTOs
{
    /// <summary>
    /// Vendor onboarding application. <see cref="IdentifierToken"/> is the raw NIN/CAC number; it is
    /// sent to the KYC provider and never persisted (NDPA data minimization).
    /// </summary>
    public record CreateVendorRequestRequest(
        string BusinessName,
        string WhatsAppNumber,
        VerificationMethod Method,
        string IdentifierToken);

    /// <summary>Result of submitting (or reading) a vendor request.</summary>
    public record VendorRequestResponse(
        Guid Id,
        VendorRequestStatus Status,
        bool NameMatch,
        DateTimeOffset CreatedAtUtc);

    /// <summary>Public shop header shown to buyers.</summary>
    public record VendorPublicResponse(
        Guid Id,
        string BusinessName,
        bool Verified,
        double AverageRating,
        int ReviewCount);

    /// <summary>Vendor self-service profile update.</summary>
    public record UpdateVendorProfileRequest(string WhatsAppNumber);

    /// <summary>Admin reject payload.</summary>
    public record RejectVendorRequestRequest(RejectionReason Reason);

    /// <summary>Admin suspend payload.</summary>
    public record SuspendVendorRequest(string Reason);

    /// <summary>Admin moderation queue row.</summary>
    public record VendorRequestQueueItem(
        Guid Id,
        Guid UserId,
        string BusinessName,
        VerificationMethod Method,
        VerificationStatus VerificationStatus,
        bool NameMatch,
        DateTimeOffset CreatedAtUtc,
        /// <summary>True when the applicant's account is currently suspended (drives Suspend vs Reinstate).</summary>
        bool Suspended);
}
