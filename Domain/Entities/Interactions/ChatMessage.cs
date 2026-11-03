using System;

namespace Domain.Entities.Interactions
{
    /// <summary>A message in the buyer-vendor conversation created from an interest lead.</summary>
    public class ChatMessage
    {
        public Guid Id { get; set; }

        /// <summary>The lead/conversation this message belongs to.</summary>
        public Guid InteractionId { get; set; }

        /// <summary>Null for system-generated messages such as payment instructions.</summary>
        public Guid? SenderUserId { get; set; }

        public string Body { get; set; } = default!;

        public bool IsSystem { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
