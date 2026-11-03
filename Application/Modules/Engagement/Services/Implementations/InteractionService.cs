using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.DTOs;
using Application.Exceptions;
using Application.Modules.Catalog.Services.Interfaces;
using Application.Modules.Engagement.DTOs;
using Application.Modules.Engagement.Services.Interfaces;
using Application.Modules.Vendors.Services.Interfaces;
using Domain.Entities.Enums;
using Domain.Entities.Interactions;
using Domain.Entities.Vendors;

namespace Application.Modules.Engagement.Services.Implementations
{
    /// <summary>
    /// Records buyer leads on-platform, creates an in-app conversation, and serves the vendor leads
    /// dashboard. Repeat taps on the same listing collapse to a single lead so counts and the
    /// cross-discovery metric stay honest.
    /// </summary>
    public class InteractionService(
        IInteractionRepository interactions,
        IProductRepository products,
        IVendorRepository vendors,
        INotificationService notifications) : IInteractionService
    {
        private const int MaxPageSize = 50;

        private static readonly PaymentAccountDetails PlaceholderPaymentAccount = new(
            "Marketplace Vendor",
            "Trade9ja Demo Bank",
            "0123456789");

        public Task<PagedResponse<ChatSummaryResponse>> GetChatsAsync(
            Guid userId, int page, int pageSize, CancellationToken ct) =>
            interactions.GetChatsAsync(
                userId, Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize), ct);

        public async Task<InterestResponse> ExpressInterestAsync(
            Guid buyerUserId, Guid productId, ExpressInterestRequest request, CancellationToken ct)
        {
            var product = await products.GetDetailAsync(productId, ct)
                ?? throw new NotFoundException("Listing not found.");

            if (product.Status != ListingStatus.Live || product.Vendor.User.Status != UserStatus.Active)
                throw new ConflictException("This listing is not currently available.");

            var vendor = product.Vendor;
            if (vendor.UserId == buyerUserId)
                throw new BusinessRuleException("You cannot express interest in your own listing.");

            var message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim();
            var offer = request.OfferPrice;

            var lead = await interactions.GetByBuyerAndProductAsync(buyerUserId, productId, ct);
            var isNewLead = lead is null;
            if (lead is not null)
            {
                lead.BuyerMessage = message;
                lead.OfferPrice = offer;
                await interactions.SaveAsync(lead, ct);
            }
            else
            {
                var isCrossDiscovery = !await interactions.HasPriorWithVendorAsync(buyerUserId, vendor.Id, ct);
                lead = new Interaction
                {
                    Id = Guid.CreateVersion7(),
                    BuyerUserId = buyerUserId,
                    VendorProfileId = vendor.Id,
                    ProductId = productId,
                    BuyerMessage = message,
                    OfferPrice = offer,
                    LeadStatus = LeadStatus.New,
                    IsCrossDiscovery = isCrossDiscovery,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };
                await interactions.AddAsync(lead, ct);
            }

            var chatMessage = message is null
                ? null
                : await interactions.AddMessageAsync(new ChatMessage
                {
                    Id = Guid.CreateVersion7(),
                    InteractionId = lead.Id,
                    SenderUserId = buyerUserId,
                    Body = message,
                    IsSystem = false,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                }, ct);

            if (isNewLead)
            {
                await notifications.NotifyAsync(
                    vendor.UserId, NotificationType.NewLead, "New lead",
                    $"A buyer is interested in \"{product.Title}\".", ct);
            }

            return new InterestResponse(lead.Id, $"/chats/{lead.Id}", chatMessage);
        }

        public async Task<PagedResponse<ChatMessageResponse>> GetChatMessagesAsync(
            Guid userId, Guid interactionId, int page, int pageSize, CancellationToken ct)
        {
            await EnsureConversationAccessAsync(userId, interactionId, ct);
            return await interactions.GetMessagesAsync(
                interactionId, Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize), ct);
        }

        public async Task<ChatMessageResponse> SendChatMessageAsync(
            Guid userId, Guid interactionId, SendChatMessageRequest request, CancellationToken ct)
        {
            await EnsureConversationAccessAsync(userId, interactionId, ct);

            var body = request.Message.Trim();
            if (string.IsNullOrWhiteSpace(body))
                throw new BusinessRuleException("Message cannot be empty.");

            return await interactions.AddMessageAsync(new ChatMessage
            {
                Id = Guid.CreateVersion7(),
                InteractionId = interactionId,
                SenderUserId = userId,
                Body = body,
                IsSystem = false,
                CreatedAtUtc = DateTimeOffset.UtcNow
            }, ct);
        }

        public async Task<PaymentInstructionsResponse> ProceedToPayAsync(
            Guid buyerUserId, Guid interactionId, CancellationToken ct)
        {
            var lead = await interactions.GetTrackedAsync(interactionId, ct)
                ?? throw new NotFoundException("Conversation not found.");
            if (lead.BuyerUserId != buyerUserId)
                throw new ForbiddenException("Only the buyer can proceed to payment for this conversation.");

            var body =
                "Hey, thanks for stopping by. Make a payment into this account: " +
                $"Account name: {PlaceholderPaymentAccount.AccountName}; " +
                $"Bank: {PlaceholderPaymentAccount.BankName}; " +
                $"Account number: {PlaceholderPaymentAccount.AccountNumber}.";

            var message = await interactions.AddMessageAsync(new ChatMessage
            {
                Id = Guid.CreateVersion7(),
                InteractionId = interactionId,
                SenderUserId = null,
                Body = body,
                IsSystem = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            }, ct);

            return new PaymentInstructionsResponse(message, PlaceholderPaymentAccount);
        }

        public async Task<PagedResponse<LeadResponse>> GetVendorLeadsAsync(
            Guid vendorUserId, LeadStatus? status, int page, int pageSize, CancellationToken ct)
        {
            var vendor = await RequireVendor(vendorUserId, ct);
            return await interactions.GetVendorLeadsAsync(
                vendor.Id, status, Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize), ct);
        }

        public async Task UpdateLeadAsync(
            Guid vendorUserId, Guid leadId, UpdateLeadRequest request, CancellationToken ct)
        {
            if (request.Status is not (LeadStatus.Viewed or LeadStatus.Closed))
                throw new BusinessRuleException("A lead can only be marked Viewed or Closed.");

            var vendor = await RequireVendor(vendorUserId, ct);

            var lead = await interactions.GetTrackedAsync(leadId, ct)
                ?? throw new NotFoundException("Lead not found.");
            if (lead.VendorProfileId != vendor.Id)
                throw new ForbiddenException("You can only manage leads for your own shop.");

            lead.LeadStatus = request.Status;
            await interactions.SaveAsync(lead, ct);
        }

        public async Task EnsureConversationAccessAsync(Guid userId, Guid interactionId, CancellationToken ct)
        {
            var lead = await interactions.GetTrackedAsync(interactionId, ct)
                ?? throw new NotFoundException("Conversation not found.");

            if (lead.BuyerUserId == userId)
                return;

            var vendor = await vendors.GetProfileByUserAsync(userId, ct);
            if (vendor?.Id == lead.VendorProfileId)
                return;

            throw new ForbiddenException("You can only access conversations you are part of.");
        }

        private async Task<VendorProfile> RequireVendor(Guid userId, CancellationToken ct) =>
            await vendors.GetProfileByUserAsync(userId, ct)
            ?? throw new ForbiddenException("You must be an approved vendor to manage leads.");
    }
}
