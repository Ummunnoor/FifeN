using System;
using Domain.Entities.Enums;

namespace Application.Modules.Engagement.DTOs
{
    /// <summary>Buyer's "I'm interested" payload; both fields are optional.</summary>
    public record ExpressInterestRequest(string? Message, decimal? OfferPrice);

    /// <summary>The recorded lead plus the pre-filled WhatsApp hand-off link.</summary>
    public record InterestResponse(Guid InteractionId, string WhatsAppUrl);

    /// <summary>A lead as shown on the vendor's leads dashboard.</summary>
    public record LeadResponse(
        Guid Id,
        Guid ProductId,
        string ProductTitle,
        string BuyerDisplayName,
        string? Message,
        decimal? OfferPrice,
        LeadStatus Status,
        bool IsCrossDiscovery,
        DateTimeOffset CreatedAtUtc);

    /// <summary>Vendor's update to a lead's progress (Viewed or Closed).</summary>
    public record UpdateLeadRequest(LeadStatus Status);
}
