using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Enums;

namespace Application.Abstractions
{
    /// <summary>
    /// Creates user notifications. The in-app record is always written; higher-value types are also
    /// pushed over WhatsApp/SMS by the notifications worker.
    /// </summary>
    public interface INotificationService
    {
        Task NotifyAsync(
            Guid recipientUserId,
            NotificationType type,
            string title,
            string body,
            CancellationToken ct = default);
    }
}
