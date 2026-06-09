using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Modules.Catalog.DTOs;
using Application.Modules.Discovery.DTOs;
using Application.Modules.Discovery.Services.Interfaces;
using Domain.Entities.Catalog;
using Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Modules.Discovery
{
    /// <summary>
    /// Read-optimized listing queries. Search uses the Postgres generated <c>tsvector</c> column (GIN
    /// indexed) for full-text matching, with an ILIKE fallback for partial matches. Only Live listings
    /// from approved, active vendors are ever returned.
    /// </summary>
    public sealed class ProductQueryRepository(FifeNDbContext db) : IProductQueryRepository
    {
        /// <summary>Projects a listing to its summary. The Review query filter limits the average to visible reviews.</summary>
        private static readonly Expression<Func<Product, ProductSummary>> ToSummary = p => new ProductSummary(
            p.Id,
            p.Title,
            p.Price.Amount,
            p.Price.Currency,
            p.PriceType,
            p.Images.OrderByDescending(i => i.IsCover).ThenBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
            p.Location.City,
            p.Location.State,
            p.Vendor.Id,
            p.Vendor.BusinessName,
            p.Vendor.VerificationStatus == VerificationStatus.Verified,
            p.Reviews.Select(r => (double?)r.Rating).Average() ?? 0d);

        private IQueryable<Product> VisibleListings() =>
            db.Products
                .AsNoTracking()
                .Where(p => p.Status == ListingStatus.Live && p.Vendor.User.Status == UserStatus.Active);

        public async Task<PagedResponse<ProductSummary>> SearchAsync(ProductQuery q, CancellationToken ct)
        {
            var listings = VisibleListings();

            if (!string.IsNullOrWhiteSpace(q.Q))
            {
                var term = q.Q.Trim();
                listings = listings.Where(p =>
                    p.SearchVector!.Matches(EF.Functions.PlainToTsQuery("english", term))
                    || EF.Functions.ILike(p.Title, $"%{term}%"));
            }

            if (q.CategoryId is { } categoryId)
                listings = listings.Where(p => p.CategoryId == categoryId);
            if (q.State is { } state)
                listings = listings.Where(p => p.Location.State == state);
            if (!string.IsNullOrWhiteSpace(q.City))
                listings = listings.Where(p => EF.Functions.ILike(p.Location.City, q.City));
            if (q.Condition is { } condition)
                listings = listings.Where(p => p.Condition == condition);
            if (q.MinPrice is { } min)
                listings = listings.Where(p => p.Price.Amount >= min);
            if (q.MaxPrice is { } max)
                listings = listings.Where(p => p.Price.Amount <= max);

            listings = q.Sort switch
            {
                "price_asc" => listings.OrderBy(p => p.Price.Amount),
                "price_desc" => listings.OrderByDescending(p => p.Price.Amount),
                "rating" => listings.OrderByDescending(p => p.Reviews.Select(r => (double?)r.Rating).Average() ?? 0d),
                _ => listings.OrderByDescending(p => p.CreatedAtUtc)
            };

            return await PageAsync(listings, q.Page, q.PageSize, ct);
        }

        public async Task<PagedResponse<ProductSummary>> NewThisWeekAsync(
            Guid? categoryId, int page, int pageSize, CancellationToken ct)
        {
            var since = DateTimeOffset.UtcNow.AddDays(-7);
            var listings = VisibleListings().Where(p => p.CreatedAtUtc >= since);
            if (categoryId is { } id)
                listings = listings.Where(p => p.CategoryId == id);

            return await PageAsync(listings.OrderByDescending(p => p.CreatedAtUtc), page, pageSize, ct);
        }

        public Task<PagedResponse<ProductSummary>> ByVendorAsync(
            Guid vendorProfileId, int page, int pageSize, CancellationToken ct)
        {
            var listings = VisibleListings()
                .Where(p => p.VendorProfileId == vendorProfileId)
                .OrderByDescending(p => p.CreatedAtUtc);
            return PageAsync(listings, page, pageSize, ct);
        }

        private static async Task<PagedResponse<ProductSummary>> PageAsync(
            IQueryable<Product> source, int page, int pageSize, CancellationToken ct)
        {
            var total = await source.CountAsync(ct);
            var items = await source
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToSummary)
                .ToListAsync(ct);

            return new PagedResponse<ProductSummary>(items, page, pageSize, total);
        }
    }
}
