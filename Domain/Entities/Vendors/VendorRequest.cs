using System;
using Domain.Entities.Enums;

namespace Domain.Entities.Vendors
{
    /// <summary>
    /// A vendor onboarding application. Created by an authenticated buyer, it carries the KYC
    /// outcome and sits in the admin queue until approved (creating a <see cref="VendorProfile"/>)
    /// or rejected (re-apply allowed).
    /// </summary>
    public class VendorRequest
    {
        public Guid Id { get; set; }

        /// <summary>Applicant user.</summary>
        public Guid UserId { get; set; }

        /// <summary>Proposed public business name (validated unique on approval).</summary>
        public string BusinessName { get; set; } = default!;

        /// <summary>Business WhatsApp number for buyer hand-off.</summary>
        public string WhatsAppNumber { get; set; } = default!;

        /// <summary>Verification method used (NIN/CAC).</summary>
        public VerificationMethod VerificationMethod { get; set; }

        /// <summary>Outcome of the KYC check run at submission.</summary>
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;

        /// <summary>Whether the KYC-returned name matched the applicant.</summary>
        public bool NameMatch { get; set; }

        /// <summary>Opaque KYC provider reference token. Never a raw identifier.</summary>
        public string? KycReference { get; set; }

        /// <summary>Queue state of the request.</summary>
        public VendorRequestStatus Status { get; set; } = VendorRequestStatus.Pending;

        /// <summary>Standardized rejection reason, when rejected.</summary>
        public RejectionReason? RejectionReason { get; set; }

        /// <summary>Admin who actioned the request.</summary>
        public Guid? ReviewedByUserId { get; set; }

        /// <summary>When the request was actioned (UTC).</summary>
        public DateTimeOffset? ReviewedAtUtc { get; set; }

        /// <summary>PostgreSQL <c>xmin</c> optimistic-concurrency token for admin actions.</summary>
        public uint Version { get; set; }

        /// <summary>When the request was submitted (UTC).</summary>
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
