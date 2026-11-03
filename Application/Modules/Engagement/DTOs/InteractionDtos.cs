using System;
using Domain.Entities.Enums;

namespace Application.Modules.Engagement.DTOs
{
    /// <summary>Buyer's "I'm interested" payload; both fields are optional.</summary>
    public record ExpressInterestRequest(string? Message, decimal? OfferPrice);

    /// <summary>The recorded lead plus the in-app chat route clients should open.</summary>
    public record InterestResponse(
        Guid InteractionId, string ChatPath, ChatMessageResponse? Message = null);

    /// <summary>A buyer or vendor chat message.</summary>
    public record SendChatMessageRequest(string Message);

    /// <summary>A conversation in the caller's chat list.</summary>
    public record ChatSummaryResponse(
        Guid Id,
        Guid ProductId,
        string ProductTitle,
        string OtherParticipantDisplayName,
        LeadStatus Status,
        DateTimeOffset CreatedAtUtc,
        ChatPreviewResponse? LatestMessage);

    /// <summary>The latest message shown in a conversation preview.</summary>
    public record ChatPreviewResponse(
        Guid? SenderUserId,
        string Body,
        bool IsSystem,
        DateTimeOffset CreatedAtUtc);

    /// <summary>Bank account details shown when the buyer is ready to pay.</summary>
    public record PaymentAccountDetails(string AccountName, string BankName, string AccountNumber);

    /// <summary>A message in an in-app buyer-vendor conversation.</summary>
    public record ChatMessageResponse(
        Guid Id,
        Guid InteractionId,
        Guid? SenderUserId,
        string SenderDisplayName,
        string Body,
        bool IsSystem,
        DateTimeOffset CreatedAtUtc);

    /// <summary>The automated payment instruction message and the account details it contains.</summary>
    public record PaymentInstructionsResponse(ChatMessageResponse Message, PaymentAccountDetails AccountDetails);

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
