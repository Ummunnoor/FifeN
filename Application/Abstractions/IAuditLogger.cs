using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Abstractions
{
    /// <summary>
    /// Writes append-only audit records for admin mutations. Every approve/reject/suspend/moderate
    /// action must be attributed through this port.
    /// </summary>
    public interface IAuditLogger
    {
        Task WriteAsync(
            Guid actorUserId,
            string action,
            string objectType,
            Guid objectId,
            string? reason = null,
            string? metadataJson = null,
            string? ipAddress = null,
            CancellationToken ct = default);
    }
}
