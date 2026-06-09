using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Exceptions;
using Application.Modules.Catalog.DTOs;
using Application.Modules.Catalog.Services.Interfaces;
using Application.Modules.Discovery.DTOs;
using Application.Modules.Discovery.Services.Interfaces;
using Application.Modules.Vendors.Services.Interfaces;
using Domain.Entities.Enums;

namespace Application.Modules.Discovery.Services.Implementations
{
    /// <summary>Validates and normalizes buyer-facing browse/search requests, then delegates to the query repository.</summary>
    public class DiscoveryService(
        IProductQueryRepository query,
        ICategoryRepository categories,
        IVendorRepository vendors) : IDiscoveryService
    {
        private const int MaxPageSize = 50;
        private static readonly HashSet<string> AllowedSorts =
            new(StringComparer.OrdinalIgnoreCase) { "recent", "price_asc", "price_desc", "rating" };

        public Task<PagedResponse<ProductSummary>> SearchAsync(ProductQuery q, CancellationToken ct)
        {
            if (!AllowedSorts.Contains(q.Sort))
                throw new InputValidationException(new Dictionary<string, string[]>
                {
                    [nameof(q.Sort)] = ["Sort must be one of: recent, price_asc, price_desc, rating."]
                });

            if (q.MinPrice is not null && q.MaxPrice is not null && q.MinPrice > q.MaxPrice)
                throw new InputValidationException(new Dictionary<string, string[]>
                {
                    [nameof(q.MinPrice)] = ["MinPrice cannot be greater than MaxPrice."]
                });

            var normalized = q with
            {
                Sort = q.Sort.ToLowerInvariant(),
                Page = Math.Max(q.Page, 1),
                PageSize = Math.Clamp(q.PageSize, 1, MaxPageSize)
            };
            return query.SearchAsync(normalized, ct);
        }

        public Task<PagedResponse<ProductSummary>> NewThisWeekAsync(Guid? categoryId, int page, int pageSize, CancellationToken ct) =>
            query.NewThisWeekAsync(categoryId, Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize), ct);

        public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken ct)
        {
            var active = await categories.GetActiveAsync(ct);
            return active.Select(c => new CategoryResponse(c.Id, c.Name, c.Slug, c.IsConsumable)).ToList();
        }

        public async Task<PagedResponse<ProductSummary>> GetVendorListingsAsync(
            Guid vendorProfileId, int page, int pageSize, CancellationToken ct)
        {
            var vendor = await vendors.GetProfileAsync(vendorProfileId, ct);
            if (vendor is null || vendor.User.Status != UserStatus.Active)
                throw new NotFoundException("Vendor not found.");

            return await query.ByVendorAsync(vendorProfileId, Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize), ct);
        }
    }
}
