using System;
using Domain.Entities.Enums;

namespace Domain.Entities.Notifications
{
    /// <summary>
    /// A message to a user. Always recorded in-app; high-value events (e.g. a new lead) are also
    /// pushed over WhatsApp/SMS. Low-priority notifications are batched to avoid overwhelming users.
    /// </summary>
    public class Notification
    {
        public Guid Id { get; set; }

        /// <summary>The user this notification is for.</summary>
        public Guid RecipientUserId { get; set; }

        /// <summary>The event type, driving templating.</summary>
        public NotificationType Type { get; set; }

        /// <summary>Channel this record was delivered over.</summary>
        public NotificationChannel Channel { get; set; }

        /// <summary>Short title.</summary>
        public string Title { get; set; } = default!;

        /// <summary>Body text.</summary>
        public string Body { get; set; } = default!;

        /// <summary>Whether the recipient has read it (in-app).</summary>
        public bool IsRead { get; set; }

        /// <summary>When the notification was created (UTC).</summary>
        public DateTimeOffset CreatedAtUtc { get; set; }

        /// <summary>When an external (WhatsApp/SMS) send completed (UTC).</summary>
        public DateTimeOffset? SentAtUtc { get; set; }
    }
}
