using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Modules.Engagement.DTOs;
using Application.Modules.Engagement.Services.Interfaces;
using Domain.Entities.Enums;
using Domain.Entities.Interactions;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Modules.Engagement
{
    /// <summary>
    /// EF Core data access for interactions (leads). The dashboard query joins the (navigation-less)
    /// interaction to its product and buyer to project a <see cref="LeadResponse"/> in a single round-trip.
    /// </summary>
    public sealed class InteractionRepository(FifeNDbContext db) : IInteractionRepository
    {
        public Task<Interaction?> GetByBuyerAndProductAsync(Guid buyerUserId, Guid productId, CancellationToken ct) =>
            db.Interactions.FirstOrDefaultAsync(i => i.BuyerUserId == buyerUserId && i.ProductId == productId, ct);

        public Task<bool> HasPriorWithVendorAsync(Guid buyerUserId, Guid vendorProfileId, CancellationToken ct) =>
            db.Interactions.AnyAsync(i => i.BuyerUserId == buyerUserId && i.VendorProfileId == vendorProfileId, ct);

        public async Task AddAsync(Interaction interaction, CancellationToken ct)
        {
            db.Interactions.Add(interaction);
            await db.SaveChangesAsync(ct);
        }

        public Task<Interaction?> GetTrackedAsync(Guid id, CancellationToken ct) =>
            db.Interactions.FirstOrDefaultAsync(i => i.Id == id, ct);

        public async Task SaveAsync(Interaction interaction, CancellationToken ct)
        {
            db.Interactions.Update(interaction);
            await db.SaveChangesAsync(ct);
        }

        public async Task<PagedResponse<LeadResponse>> GetVendorLeadsAsync(
            Guid vendorProfileId, LeadStatus? status, int page, int pageSize, CancellationToken ct)
        {
            var leads = db.Interactions.AsNoTracking().Where(i => i.VendorProfileId == vendorProfileId);
            if (status is { } s)
                leads = leads.Where(i => i.LeadStatus == s);

            var total = await leads.CountAsync(ct);

            var rows = await (
                from i in leads
                join p in db.Products.AsNoTracking() on i.ProductId equals p.Id
                join u in db.Users.AsNoTracking() on i.BuyerUserId equals u.Id
                orderby i.CreatedAtUtc descending
                select new
                {
                    i.Id,
                    i.ProductId,
                    p.Title,
                    u.FirstName,
                    u.LastName,
                    i.BuyerMessage,
                    i.OfferPrice,
                    i.LeadStatus,
                    i.IsCrossDiscovery,
                    i.CreatedAtUtc
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var items = rows
                .Select(r => new LeadResponse(
                    r.Id, r.ProductId, r.Title, $"{r.FirstName} {r.LastName}".Trim(),
                    r.BuyerMessage, r.OfferPrice, r.LeadStatus, r.IsCrossDiscovery, r.CreatedAtUtc))
                .ToList();

            return new PagedResponse<LeadResponse>(items, page, pageSize, total);
        }

        public async Task<PagedResponse<ChatSummaryResponse>> GetChatsAsync(
            Guid userId, int page, int pageSize, CancellationToken ct)
        {
            var chats =
                from i in db.Interactions.AsNoTracking()
                join p in db.Products.AsNoTracking() on i.ProductId equals p.Id
                join v in db.VendorProfiles.AsNoTracking() on i.VendorProfileId equals v.Id
                join buyer in db.Users.AsNoTracking() on i.BuyerUserId equals buyer.Id
                join vendor in db.Users.AsNoTracking() on v.UserId equals vendor.Id
                where i.BuyerUserId == userId || v.UserId == userId
                select new { Interaction = i, Product = p, Buyer = buyer, Vendor = vendor };

            var total = await chats.CountAsync(ct);
            var rows = await chats
                .OrderByDescending(row => row.Interaction.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(row => new
                {
                    row.Interaction.Id,
                    row.Interaction.ProductId,
                    ProductTitle = row.Product.Title,
                    OtherParticipantDisplayName = row.Interaction.BuyerUserId == userId
                        ? row.Vendor.FirstName + " " + row.Vendor.LastName
                        : row.Buyer.FirstName + " " + row.Buyer.LastName,
                    row.Interaction.LeadStatus,
                    row.Interaction.CreatedAtUtc,
                    LatestMessage = db.ChatMessages.AsNoTracking()
                        .Where(message => message.InteractionId == row.Interaction.Id)
                        .OrderByDescending(message => message.CreatedAtUtc)
                        .Select(message => new
                        {
                            message.SenderUserId,
                            message.Body,
                            message.IsSystem,
                            message.CreatedAtUtc
                        })
                        .FirstOrDefault()
                })
                .ToListAsync(ct);

            var items = rows.Select(row => new ChatSummaryResponse(
                row.Id,
                row.ProductId,
                row.ProductTitle,
                row.OtherParticipantDisplayName.Trim(),
                row.LeadStatus,
                row.CreatedAtUtc,
                row.LatestMessage is null
                    ? null
                    : new ChatPreviewResponse(
                        row.LatestMessage.SenderUserId,
                        row.LatestMessage.Body,
                        row.LatestMessage.IsSystem,
                        row.LatestMessage.CreatedAtUtc)))
                .ToList();

            return new PagedResponse<ChatSummaryResponse>(items, page, pageSize, total);
        }

        public async Task<PagedResponse<ChatMessageResponse>> GetMessagesAsync(
            Guid interactionId, int page, int pageSize, CancellationToken ct)
        {
            var messages = db.ChatMessages.AsNoTracking().Where(m => m.InteractionId == interactionId);
            var total = await messages.CountAsync(ct);

            var rows = await messages
                .OrderByDescending(m => m.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .GroupJoin(
                    db.Users.AsNoTracking(),
                    message => message.SenderUserId,
                    user => user.Id,
                    (message, users) => new { Message = message, User = users.FirstOrDefault() })
                .ToListAsync(ct);

            var items = rows
                .OrderBy(r => r.Message.CreatedAtUtc)
                .Select(r => new ChatMessageResponse(
                    r.Message.Id,
                    r.Message.InteractionId,
                    r.Message.SenderUserId,
                    r.Message.IsSystem ? "Trade9ja" : $"{r.User!.FirstName} {r.User.LastName}".Trim(),
                    r.Message.Body,
                    r.Message.IsSystem,
                    r.Message.CreatedAtUtc))
                .ToList();

            return new PagedResponse<ChatMessageResponse>(items, page, pageSize, total);
        }

        public async Task<ChatMessageResponse> AddMessageAsync(ChatMessage message, CancellationToken ct)
        {
            db.ChatMessages.Add(message);
            await db.SaveChangesAsync(ct);

            var sender = message.SenderUserId.HasValue
                ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == message.SenderUserId.Value, ct)
                : null;
            var displayName = message.IsSystem ? "Trade9ja" : $"{sender!.FirstName} {sender.LastName}".Trim();

            return new ChatMessageResponse(
                message.Id,
                message.InteractionId,
                message.SenderUserId,
                displayName,
                message.Body,
                message.IsSystem,
                message.CreatedAtUtc);
        }
    }
}
