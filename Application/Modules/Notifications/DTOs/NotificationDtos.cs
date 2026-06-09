using System;
using Domain.Entities.Enums;

namespace Application.Modules.Notifications.DTOs
{
    /// <summary>A user's in-app notification.</summary>
    public record NotificationResponse(
        Guid Id, NotificationType Type, string Title, string Body, bool IsRead, DateTimeOffset CreatedAtUtc);

    /// <summary>The caller's unread notification count, for a badge.</summary>
    public record UnreadCountResponse(int Count);
}
