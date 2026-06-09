using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Modules.Catalog.Services.Interfaces;
using Domain.Entities.Catalog;
using Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Modules.Catalog
{
    /// <summary>EF Core data access for listings and their images.</summary>
    public sealed class ProductRepository(FifeNDbContext db) : IProductRepository
    {
        public async Task AddAsync(Product product, CancellationToken ct)
        {
            db.Products.Add(product);
            await db.SaveChangesAsync(ct);
        }

        public Task<Product?> GetTrackedAsync(Guid id, CancellationToken ct) =>
            db.Products
                .Include(p => p.Images)
                .Include(p => p.Vendor)
                .FirstOrDefaultAsync(p => p.Id == id, ct);

        public Task<Product?> GetDetailAsync(Guid id, CancellationToken ct) =>
            db.Products
                .AsNoTracking()
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Vendor).ThenInclude(v => v.User)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id, ct);

        public async Task SaveAsync(Product product, CancellationToken ct)
        {
            db.Products.Update(product);
            await db.SaveChangesAsync(ct);
        }

        public Task<int> CountActiveByVendorAsync(Guid vendorProfileId, CancellationToken ct) =>
            db.Products.CountAsync(
                p => p.VendorProfileId == vendorProfileId
                     && (p.Status == ListingStatus.Live || p.Status == ListingStatus.Unavailable), ct);

        public Task IncrementViewAsync(Guid id, CancellationToken ct) =>
            db.Products
                .Where(p => p.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1), ct);

        public async Task AddImagesAsync(IEnumerable<ProductImage> images, CancellationToken ct)
        {
            db.ProductImages.AddRange(images);
            await db.SaveChangesAsync(ct);
        }

        public Task<int> CountImagesAsync(Guid productId, CancellationToken ct) =>
            db.ProductImages.CountAsync(i => i.ProductId == productId, ct);
    }
}
