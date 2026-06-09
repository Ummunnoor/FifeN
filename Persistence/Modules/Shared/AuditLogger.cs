using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Domain.Entities.TrustSafety;

namespace Persistence.Modules.Shared
{
    /// <summary>Append-only writer for the immutable <see cref="AuditLog"/>.</summary>
    public sealed class AuditLogger(FifeNDbContext db) : IAuditLogger
    {
        public async Task WriteAsync(
            Guid actorUserId,
            string action,
            string objectType,
            Guid objectId,
            string? reason = null,
            string? metadataJson = null,
            string? ipAddress = null,
            CancellationToken ct = default)
        {
            db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.CreateVersion7(),
                ActorUserId = actorUserId,
                Action = action,
                ObjectType = objectType,
                ObjectId = objectId,
                Reason = reason,
                MetadataJson = metadataJson,
                IpAddress = ipAddress,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }
    }
}
