using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Domain.Entities.Enums;
using Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Modules.Shared
{
    /// <summary>
    /// Writes in-app notification records. External (WhatsApp/SMS) fan-out is handled separately by the
    /// notifications worker; this guarantees the in-app record exists.
    /// </summary>
    public sealed class NotificationService(FifeNDbContext db) : INotificationService
    {
        public async Task NotifyAsync(
            Guid recipientUserId, NotificationType type, string title, string body, CancellationToken ct = default)
        {
            db.Notifications.Add(new Notification
            {
                Id = Guid.CreateVersion7(),
                RecipientUserId = recipientUserId,
                Type = type,
                Channel = NotificationChannel.InApp,
                Title = title,
                Body = body,
                IsRead = false,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }

        public async Task NotifyAdminsAsync(
            NotificationType type, string title, string body, CancellationToken ct = default)
        {
            var adminIds = await db.Users
                .AsNoTracking()
                .Where(u => u.IsAdmin)
                .Select(u => u.Id)
                .ToListAsync(ct);

            if (adminIds.Count == 0)
                return;

            var now = DateTimeOffset.UtcNow;
            foreach (var adminId in adminIds)
            {
                db.Notifications.Add(new Notification
                {
                    Id = Guid.CreateVersion7(),
                    RecipientUserId = adminId,
                    Type = type,
                    Channel = NotificationChannel.InApp,
                    Title = title,
                    Body = body,
                    IsRead = false,
                    CreatedAtUtc = now
                });
            }
            await db.SaveChangesAsync(ct);
        }
    }
}
