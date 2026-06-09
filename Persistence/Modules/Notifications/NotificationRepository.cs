using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Modules.Notifications.DTOs;
using Application.Modules.Notifications.Services.Interfaces;
using Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Modules.Notifications
{
    /// <summary>EF Core data access for the recipient-facing notification centre.</summary>
    public sealed class NotificationRepository(FifeNDbContext db) : INotificationRepository
    {
        public async Task<PagedResponse<NotificationResponse>> GetForUserAsync(
            Guid userId, int page, int pageSize, CancellationToken ct)
        {
            var query = db.Notifications.AsNoTracking().Where(n => n.RecipientUserId == userId);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(n => n.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationResponse(n.Id, n.Type, n.Title, n.Body, n.IsRead, n.CreatedAtUtc))
                .ToListAsync(ct);

            return new PagedResponse<NotificationResponse>(items, page, pageSize, total);
        }

        public Task<int> CountUnreadAsync(Guid userId, CancellationToken ct) =>
            db.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead, ct);

        public Task<Notification?> GetTrackedAsync(Guid id, CancellationToken ct) =>
            db.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);

        public async Task SaveAsync(Notification notification, CancellationToken ct)
        {
            db.Notifications.Update(notification);
            await db.SaveChangesAsync(ct);
        }

        public Task MarkAllReadAsync(Guid userId, CancellationToken ct) =>
            db.Notifications
                .Where(n => n.RecipientUserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }
}
