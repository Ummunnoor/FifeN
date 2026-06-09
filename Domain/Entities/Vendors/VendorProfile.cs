using System;
using System.Collections.Generic;
using Domain.Entities.Catalog;
using Domain.Entities.Enums;
using Domain.Entities.Identity;

namespace Domain.Entities.Vendors
{
    /// <summary>
    /// The 1:1 vendor extension of a <see cref="User"/>, created when an admin approves a
    /// <see cref="VendorRequest"/>. Holds the public business identity, KYC results (never raw
    /// identifiers), and the trust tier that governs listing moderation.
    /// </summary>
    public class VendorProfile
    {
        public Guid Id { get; set; }

        /// <summary>Owning user (unique — one profile per account).</summary>
        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        /// <summary>Public business name. Unique, case-insensitive (stored as citext).</summary>
        public string BusinessName { get; set; } = default!;

        /// <summary>Business WhatsApp number buyers are handed off to; defaults from the login phone but overridable.</summary>
        public string WhatsAppNumber { get; set; } = default!;

        /// <summary>Whether identity was verified by NIN (individual) or CAC (business).</summary>
        public VerificationMethod VerificationMethod { get; set; }

        /// <summary>Outcome of KYC verification.</summary>
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;

        /// <summary>Name returned by the KYC provider, used for the name-match check.</summary>
        public string? VerifiedName { get; set; }

        /// <summary>Whether the verified name matched the applicant's name.</summary>
        public bool NameMatch { get; set; }

        /// <summary>Confidence of the name match, where the provider supplies one.</summary>
        public decimal? NameMatchConfidence { get; set; }

        /// <summary>Opaque provider reference token. Never holds a raw NIN/CAC number (NDPA).</summary>
        public string? KycReference { get; set; }

        /// <summary>Trust tier; drives the probation-to-trusted graduation.</summary>
        public TrustTier TrustTier { get; set; } = TrustTier.Pending;

        /// <summary>Count of approved listings; graduates the vendor to Trusted once it reaches the probation threshold.</summary>
        public int ApprovedListingCount { get; set; }

        /// <summary>PostgreSQL <c>xmin</c> optimistic-concurrency token.</summary>
        public uint Version { get; set; }

        /// <summary>When the profile was created (UTC).</summary>
        public DateTimeOffset CreatedAtUtc { get; set; }

        /// <summary>When the vendor was approved (UTC).</summary>
        public DateTimeOffset? ApprovedAtUtc { get; set; }

        // Navigation properties
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
