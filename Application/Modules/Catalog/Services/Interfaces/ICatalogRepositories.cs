using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Catalog;

namespace Application.Modules.Catalog.Services.Interfaces
{
    /// <summary>Data access for listings and their images.</summary>
    public interface IProductRepository
    {
        Task AddAsync(Product product, CancellationToken ct);

        /// <summary>Loads a listing for mutation (tracked), including images.</summary>
        Task<Product?> GetTrackedAsync(Guid id, CancellationToken ct);

        /// <summary>Loads a listing for public detail (no tracking), including vendor, category, images.</summary>
        Task<Product?> GetDetailAsync(Guid id, CancellationToken ct);

        Task SaveAsync(Product product, CancellationToken ct);

        /// <summary>Counts a vendor's non-terminal listings (Live + Unavailable) for the listing cap.</summary>
        Task<int> CountActiveByVendorAsync(Guid vendorProfileId, CancellationToken ct);

        Task IncrementViewAsync(Guid id, CancellationToken ct);

        Task AddImagesAsync(IEnumerable<ProductImage> images, CancellationToken ct);

        Task<int> CountImagesAsync(Guid productId, CancellationToken ct);
    }

    /// <summary>Read access for categories.</summary>
    public interface ICategoryRepository
    {
        Task<Category?> GetAsync(Guid id, CancellationToken ct);
        Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken ct);
    }
}
