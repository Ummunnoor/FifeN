using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Modules.Engagement.DTOs;
using Domain.Entities.Enums;
using Domain.Entities.Interactions;

namespace Application.Modules.Engagement.Services.Interfaces
{
    /// <summary>Buyer lead capture, in-app chat, and the vendor leads dashboard.</summary>
    public interface IInteractionService
    {
        Task<InterestResponse> ExpressInterestAsync(
            Guid buyerUserId, Guid productId, ExpressInterestRequest request, CancellationToken ct);

        Task<PagedResponse<ChatSummaryResponse>> GetChatsAsync(
            Guid userId, int page, int pageSize, CancellationToken ct);

        Task<PagedResponse<ChatMessageResponse>> GetChatMessagesAsync(
            Guid userId, Guid interactionId, int page, int pageSize, CancellationToken ct);

        Task<ChatMessageResponse> SendChatMessageAsync(
            Guid userId, Guid interactionId, SendChatMessageRequest request, CancellationToken ct);

        Task<PaymentInstructionsResponse> ProceedToPayAsync(
            Guid buyerUserId, Guid interactionId, CancellationToken ct);

        Task EnsureConversationAccessAsync(Guid userId, Guid interactionId, CancellationToken ct);

        Task<PagedResponse<LeadResponse>> GetVendorLeadsAsync(
            Guid vendorUserId, LeadStatus? status, int page, int pageSize, CancellationToken ct);

        Task UpdateLeadAsync(Guid vendorUserId, Guid leadId, UpdateLeadRequest request, CancellationToken ct);
    }

    /// <summary>Data access for interactions (leads).</summary>
    public interface IInteractionRepository
    {
        /// <summary>The buyer's existing lead for a listing, if any (one lead per buyer per listing).</summary>
        Task<Interaction?> GetByBuyerAndProductAsync(Guid buyerUserId, Guid productId, CancellationToken ct);

        /// <summary>True when the buyer has any prior interaction with this vendor (drives cross-discovery).</summary>
        Task<bool> HasPriorWithVendorAsync(Guid buyerUserId, Guid vendorProfileId, CancellationToken ct);

        Task AddAsync(Interaction interaction, CancellationToken ct);

        /// <summary>Loads a lead for mutation (tracked).</summary>
        Task<Interaction?> GetTrackedAsync(Guid id, CancellationToken ct);

        Task SaveAsync(Interaction interaction, CancellationToken ct);

        /// <summary>The vendor's leads, newest first, projected with product title and buyer name.</summary>
        Task<PagedResponse<LeadResponse>> GetVendorLeadsAsync(
            Guid vendorProfileId, LeadStatus? status, int page, int pageSize, CancellationToken ct);

        /// <summary>The caller's conversations, newest first, with the latest message preview.</summary>
        Task<PagedResponse<ChatSummaryResponse>> GetChatsAsync(
            Guid userId, int page, int pageSize, CancellationToken ct);

        Task<PagedResponse<ChatMessageResponse>> GetMessagesAsync(
            Guid interactionId, int page, int pageSize, CancellationToken ct);

        Task<ChatMessageResponse> AddMessageAsync(ChatMessage message, CancellationToken ct);
    }
}
