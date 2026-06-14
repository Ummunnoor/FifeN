using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.DTOs;
using Application.Modules.Catalog.DTOs;

namespace Application.Modules.Catalog.Services.Interfaces
{
    /// <summary>Vendor-facing listing management and public listing detail.</summary>
    public interface IProductService
    {
        Task<ProductDetail> CreateAsync(Guid userId, CreateProductRequest request, CancellationToken ct);

        /// <summary>The authenticated vendor's own listings, all statuses, for their management dashboard.</summary>
        Task<PagedResponse<ProductSummary>> GetMyListingsAsync(Guid userId, int page, int pageSize, CancellationToken ct);
        Task<ProductDetail> UpdateAsync(Guid userId, Guid productId, UpdateProductRequest request, CancellationToken ct);
        Task ChangeStatusAsync(Guid userId, Guid productId, ChangeStatusRequest request, CancellationToken ct);

        /// <summary>Soft-deletes (archives) a listing. Owners or admins may call.</summary>
        Task DeleteAsync(Guid userId, bool isAdmin, Guid productId, CancellationToken ct);

        Task<IReadOnlyList<string>> AddImagesAsync(
            Guid userId, Guid productId, IReadOnlyList<ImageUpload> images, CancellationToken ct);

        /// <summary>Public detail read; increments the view count and hides non-visible listings.</summary>
        Task<ProductDetail> GetPublicDetailAsync(Guid productId, CancellationToken ct);
    }

    /// <summary>Admin listing moderation.</summary>
    public interface IProductAdminService
    {
        /// <summary>Probation listings awaiting moderation, oldest first.</summary>
        Task<PagedResponse<ProductSummary>> GetModerationQueueAsync(int page, int pageSize, CancellationToken ct);
        Task ModerateAsync(Guid adminUserId, Guid productId, ModerateProductRequest request, string? ipAddress, CancellationToken ct);
    }
}
