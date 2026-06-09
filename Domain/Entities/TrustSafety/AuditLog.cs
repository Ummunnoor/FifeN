using System;

namespace Domain.Entities.TrustSafety
{
    /// <summary>
    /// An append-only record of an admin mutation. Rows are never updated or deleted; immutability
    /// is enforced at the repository layer and by a database rule revoking UPDATE/DELETE in production.
    /// </summary>
    public class AuditLog
    {
        public Guid Id { get; set; }

        /// <summary>The admin who performed the action.</summary>
        public Guid ActorUserId { get; set; }

        /// <summary>Action key, e.g. "VendorRequest.Approve".</summary>
        public string Action { get; set; } = default!;

        /// <summary>Type of the affected object, e.g. "VendorRequest".</summary>
        public string ObjectType { get; set; } = default!;

        /// <summary>Id of the affected object.</summary>
        public Guid ObjectId { get; set; }

        /// <summary>Standardized or free-text reason supplied for the action.</summary>
        public string? Reason { get; set; }

        /// <summary>Optional jsonb before/after snapshot.</summary>
        public string? MetadataJson { get; set; }

        /// <summary>IP the action originated from.</summary>
        public string? IpAddress { get; set; }

        /// <summary>When the action occurred (UTC).</summary>
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
