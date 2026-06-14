using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.DTOs;
using Application.Exceptions;
using Application.Modules.Catalog.DTOs;
using Application.Modules.Catalog.Services.Interfaces;
using Application.Modules.Discovery.Services.Interfaces;
using Domain.Entities.Catalog;
using Domain.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Modules.Catalog.Services.Implementations
{
    /// <summary>Admin listing moderation: approve probation listings, hide, or remove. All audited.</summary>
    public class ProductAdminService(
        IProductRepository products,
        IProductQueryRepository productQuery,
        IAuditLogger audit,
        INotificationService notifications,
        ILogger<ProductAdminService> logger) : IProductAdminService
    {
        private const int ProbationGraduationThreshold = 3;
        private const int MaxPageSize = 50;

        public Task<PagedResponse<ProductSummary>> GetModerationQueueAsync(int page, int pageSize, CancellationToken ct) =>
            productQuery.PendingModerationAsync(Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize), ct);

        public async Task ModerateAsync(
            Guid adminUserId, Guid productId, ModerateProductRequest request, string? ipAddress, CancellationToken ct)
        {
            var product = await products.GetTrackedAsync(productId, ct)
                ?? throw new NotFoundException("Listing not found.");

            switch (request.Status)
            {
                case ListingStatus.Live:
                    ApproveListing(product);
                    await Notify(product, NotificationType.ListingApproved,
                        "Listing approved", $"Your listing \"{product.Title}\" is now live.", notifications, ct);
                    break;

                case ListingStatus.Unavailable:
                case ListingStatus.Removed:
                    product.Status = request.Status;
                    await Notify(product, NotificationType.ListingTakenDown,
                        "Listing taken down", $"Your listing \"{product.Title}\" was taken down. Reason: {request.Reason}.",
                        notifications, ct);
                    break;

                default:
                    throw new BusinessRuleException("Moderation can only set a listing Live, Unavailable, or Removed.");
            }

            product.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await products.SaveAsync(product, ct);

            await audit.WriteAsync(adminUserId, "Product.Moderate", nameof(Product), product.Id,
                reason: request.Reason,
                metadataJson: $"{{\"status\":\"{request.Status}\"}}",
                ipAddress: ipAddress, ct: ct);

            logger.LogInformation("Listing {ProductId} moderated to {Status} by {AdminId}.",
                productId, request.Status, adminUserId);
        }

        private static void ApproveListing(Product product)
        {
            var wasPreModeration = product.Status != ListingStatus.Live;
            product.Status = ListingStatus.Live;

            // Graduate probation vendors once their first listings are approved.
            if (wasPreModeration && product.Vendor.TrustTier != TrustTier.Trusted)
            {
                product.Vendor.ApprovedListingCount++;
                if (product.Vendor.ApprovedListingCount >= ProbationGraduationThreshold)
                    product.Vendor.TrustTier = TrustTier.Trusted;
            }
        }

        private static Task Notify(
            Product product, NotificationType type, string title, string body,
            INotificationService notifications, CancellationToken ct) =>
            notifications.NotifyAsync(product.Vendor.UserId, type, title, body, ct);
    }
}
