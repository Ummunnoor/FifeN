using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Modules.Catalog.DTOs;
using Application.Modules.Discovery.DTOs;

namespace Application.Modules.Discovery.Services.Interfaces
{
    /// <summary>Buyer-facing browse, search, fresh-supply feed, categories, and public shop pages.</summary>
    public interface IDiscoveryService
    {
        Task<PagedResponse<ProductSummary>> SearchAsync(ProductQuery query, CancellationToken ct);
        Task<PagedResponse<ProductSummary>> NewThisWeekAsync(Guid? categoryId, int page, int pageSize, CancellationToken ct);
        Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken ct);
        Task<PagedResponse<ProductSummary>> GetVendorListingsAsync(Guid vendorProfileId, int page, int pageSize, CancellationToken ct);
    }

    /// <summary>Read-optimized listing queries (full-text search, filters, projections).</summary>
    public interface IProductQueryRepository
    {
        Task<PagedResponse<ProductSummary>> SearchAsync(ProductQuery query, CancellationToken ct);
        Task<PagedResponse<ProductSummary>> NewThisWeekAsync(Guid? categoryId, int page, int pageSize, CancellationToken ct);
        Task<PagedResponse<ProductSummary>> ByVendorAsync(Guid vendorProfileId, int page, int pageSize, CancellationToken ct);
    }
}
