using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Modules.Notifications.DTOs;
using Domain.Entities.Notifications;

namespace Application.Modules.Notifications.Services.Interfaces
{
    /// <summary>
    /// The recipient-facing notification centre: listing, unread count, and marking read. The write
    /// side (creating notifications) is the cross-cutting <see cref="Abstractions.INotificationService"/>.
    /// </summary>
    public interface INotificationFeedService
    {
        Task<PagedResponse<NotificationResponse>> GetFeedAsync(Guid userId, int page, int pageSize, CancellationToken ct);
        Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct);
        Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct);
        Task MarkAllReadAsync(Guid userId, CancellationToken ct);
    }

    /// <summary>Data access for the notification centre.</summary>
    public interface INotificationRepository
    {
        Task<PagedResponse<NotificationResponse>> GetForUserAsync(Guid userId, int page, int pageSize, CancellationToken ct);
        Task<int> CountUnreadAsync(Guid userId, CancellationToken ct);
        Task<Notification?> GetTrackedAsync(Guid id, CancellationToken ct);
        Task SaveAsync(Notification notification, CancellationToken ct);
        Task MarkAllReadAsync(Guid userId, CancellationToken ct);
    }
}
