using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Modules.Engagement.DTOs;
using Application.Modules.Engagement.Services.Interfaces;
using Domain.Entities.Reviews;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Modules.Engagement
{
    /// <summary>
    /// EF Core data access for reviews. The model has a <c>Status == Visible</c> query filter, so public
    /// reads see only visible reviews; the duplicate and moderation lookups opt out via
    /// <see cref="EntityFrameworkQueryableExtensions.IgnoreQueryFilters{TEntity}"/>.
    /// </summary>
    public sealed class ReviewRepository(FifeNDbContext db) : IReviewRepository
    {
        // Ignores the Visible-only filter so a hidden/removed prior review still blocks a re-review
        // (and so the unique (author, product) index is never violated on insert).
        public Task<bool> ExistsByAuthorAndProductAsync(Guid authorUserId, Guid productId, CancellationToken ct) =>
            db.Reviews.IgnoreQueryFilters()
                .AnyAsync(r => r.AuthorUserId == authorUserId && r.ProductId == productId, ct);

        public async Task AddAsync(Review review, CancellationToken ct)
        {
            db.Reviews.Add(review);
            await db.SaveChangesAsync(ct);
        }

        public Task<Review?> GetTrackedAsync(Guid id, CancellationToken ct) =>
            db.Reviews.FirstOrDefaultAsync(r => r.Id == id, ct);

        public Task<Review?> GetForModerationAsync(Guid id, CancellationToken ct) =>
            db.Reviews.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id, ct);

        public async Task SaveAsync(Review review, CancellationToken ct)
        {
            db.Reviews.Update(review);
            await db.SaveChangesAsync(ct);
        }

        public async Task<PagedResponse<ReviewResponse>> GetForProductAsync(
            Guid productId, int page, int pageSize, CancellationToken ct)
        {
            // The Visible-only query filter applies here.
            var query = db.Reviews.AsNoTracking().Where(r => r.ProductId == productId);

            var total = await query.CountAsync(ct);

            var rows = await (
                from r in query
                join u in db.Users.AsNoTracking() on r.AuthorUserId equals u.Id
                orderby r.CreatedAtUtc descending
                select new { r.Id, u.FirstName, u.LastName, r.Rating, r.Text, r.CreatedAtUtc })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var items = rows
                .Select(r => new ReviewResponse(
                    r.Id, $"{r.FirstName} {r.LastName}".Trim(), r.Rating, r.Text, true, r.CreatedAtUtc))
                .ToList();

            return new PagedResponse<ReviewResponse>(items, page, pageSize, total);
        }
    }
}
