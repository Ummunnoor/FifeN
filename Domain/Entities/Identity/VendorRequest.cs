using System;
using Domain.Entities.Enums;

namespace Domain.Entities.Identity
{
    /// <summary>
    /// Represents a pending vendor registration request.
    /// Users must be approved before becoming vendors and getting a Shop.
    /// </summary>
    public class VendorRequest
    {
        /// <summary>Primary key identifier</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Reference to the User requesting vendor status</summary>
        public required string UserId { get; set; }

        /// <summary>Navigation property to User</summary>
        public User? User { get; set; }

        /// <summary>Business email address</summary>
        public required string BusinessEmail { get; set; }

        /// <summary>Business name/shop name proposed</summary>
        public required string BusinessName { get; set; }

        /// <summary>Business description</summary>
        public required string Description { get; set; }

        /// <summary>Business registration number (optional)</summary>
        public string? RegistrationNumber { get; set; }

        /// <summary>Years of business experience</summary>
        public int? YearsOfExperience { get; set; }

        /// <summary>Current status of the request</summary>
        public VendorRequestStatus Status { get; set; } = VendorRequestStatus.Pending;

        /// <summary>Admin notes/feedback on the request</summary>
        public string? AdminNotes { get; set; }

        /// <summary>Rejection reason (if rejected)</summary>
        public string? RejectionReason { get; set; }

        /// <summary>When the request was created</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>When the request was reviewed/processed</summary>
        public DateTime? ReviewedAt { get; set; }

        /// <summary>When the request expires if not reviewed</summary>
        public DateTime? ExpiresAt { get; set; }
    }
}