using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Exceptions;
using Application.Modules.Notifications.DTOs;
using Application.Modules.Notifications.Services.Interfaces;

namespace Application.Modules.Notifications.Services.Implementations
{
    /// <summary>The recipient-facing notification centre. Users only ever see and act on their own records.</summary>
    public class NotificationFeedService(INotificationRepository notifications) : INotificationFeedService
    {
        private const int MaxPageSize = 50;

        public Task<PagedResponse<NotificationResponse>> GetFeedAsync(
            Guid userId, int page, int pageSize, CancellationToken ct) =>
            notifications.GetForUserAsync(userId, Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize), ct);

        public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct) =>
            notifications.CountUnreadAsync(userId, ct);

        public async Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct)
        {
            var notification = await notifications.GetTrackedAsync(notificationId, ct)
                ?? throw new NotFoundException("Notification not found.");
            if (notification.RecipientUserId != userId)
                throw new ForbiddenException("You can only update your own notifications.");

            if (notification.IsRead)
                return;

            notification.IsRead = true;
            await notifications.SaveAsync(notification, ct);
        }

        public Task MarkAllReadAsync(Guid userId, CancellationToken ct) =>
            notifications.MarkAllReadAsync(userId, ct);
    }
}
