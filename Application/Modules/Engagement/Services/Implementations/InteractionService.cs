using System;
using System.Linq;
using System.Text;
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
    /// Records buyer leads on-platform <em>before</em> the WhatsApp hand-off and serves the vendor leads
    /// dashboard. The lead is always persisted first so the interaction is captured even if the buyer
    /// never opens the chat. Repeat taps on the same listing collapse to a single lead so counts and the
    /// cross-discovery metric stay honest.
    /// </summary>
    public class InteractionService(
        IInteractionRepository interactions,
        IProductRepository products,
        IVendorRepository vendors,
        INotificationService notifications) : IInteractionService
    {
        private const int MaxPageSize = 50;

        public async Task<InterestResponse> ExpressInterestAsync(
            Guid buyerUserId, Guid productId, ExpressInterestRequest request, CancellationToken ct)
        {
            var product = await products.GetDetailAsync(productId, ct)
                ?? throw new NotFoundException("Listing not found.");

            if (product.Status != ListingStatus.Live || product.Vendor.User.Status != UserStatus.Active)
                throw new ConflictException("This listing is not currently available.");

            var vendor = product.Vendor;

            // A vendor can't be a lead on their own listing — it would corrupt lead counts, the
            // cross-discovery metric, and review eligibility (and produces a wa.me link to oneself).
            if (vendor.UserId == buyerUserId)
                throw new BusinessRuleException("You cannot express interest in your own listing.");

            var message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim();
            var offer = request.OfferPrice;

            // One lead per buyer per listing. A repeat tap refreshes the pre-fill but is not a new lead
            // (keeps lead counts and cross-discovery honest) and does not re-notify the vendor.
            var lead = await interactions.GetByBuyerAndProductAsync(buyerUserId, productId, ct);
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

                await notifications.NotifyAsync(
                    vendor.UserId, NotificationType.NewLead, "New lead",
                    $"A buyer is interested in \"{product.Title}\".", ct);
            }

            var url = BuildWhatsAppUrl(vendor.WhatsAppNumber, vendor.BusinessName, product.Title, message, offer);
            return new InterestResponse(lead.Id, url);
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

        private async Task<VendorProfile> RequireVendor(Guid userId, CancellationToken ct) =>
            await vendors.GetProfileByUserAsync(userId, ct)
            ?? throw new ForbiddenException("You must be an approved vendor to manage leads.");

        /// <summary>
        /// Builds a <c>wa.me</c> deep link with a pre-filled message. The number is reduced to digits
        /// (wa.me rejects '+' and spaces) and the message is percent-encoded.
        /// </summary>
        private static string BuildWhatsAppUrl(
            string whatsAppNumber, string businessName, string productTitle, string? message, decimal? offer)
        {
            var digits = new string(whatsAppNumber.Where(char.IsDigit).ToArray());

            var text = new StringBuilder();
            text.Append($"Hi {businessName}, I'm interested in \"{productTitle}\" on TradeNaija.");
            if (offer is { } amount)
                text.Append($" I'd like to offer ₦{amount:N0}.");
            if (!string.IsNullOrWhiteSpace(message))
                text.Append(' ').Append(message);

            return $"https://wa.me/{digits}?text={Uri.EscapeDataString(text.ToString())}";
        }
    }
}
