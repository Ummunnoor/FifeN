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
    }
}
