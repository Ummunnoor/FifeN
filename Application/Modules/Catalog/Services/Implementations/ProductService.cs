using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.Exceptions;
using Application.Modules.Catalog.DTOs;
using Application.Modules.Catalog.Services.Interfaces;
using Application.Modules.Vendors.Services.Interfaces;
using Domain.Entities.Catalog;
using Domain.Entities.Enums;
using Domain.Entities.Vendors;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Modules.Catalog.Services.Implementations
{
    /// <summary>Vendor listing management and public listing detail.</summary>
    public class ProductService(
        IProductRepository products,
        ICategoryRepository categories,
        IVendorRepository vendors,
        IImageStorageService imageStorage,
        ILogger<ProductService> logger) : IProductService
    {
        private const int MaxActiveListings = 50;
        private const int MaxImages = 5;
        private const long MaxImageBytes = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedImageTypes =
            new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

        public async Task<ProductDetail> CreateAsync(Guid userId, CreateProductRequest request, CancellationToken ct)
        {
            var vendor = await RequireVendor(userId, ct);
            var category = await RequireCategory(request.CategoryId, ct);
            EnsureConditionAllowed(request.Condition, category);

            if (await products.CountActiveByVendorAsync(vendor.Id, ct) >= MaxActiveListings)
                throw new BusinessRuleException(
                    $"You have reached the maximum of {MaxActiveListings} active listings.");

            var now = DateTimeOffset.UtcNow;
            var product = new Product
            {
                Id = Guid.CreateVersion7(),
                VendorProfileId = vendor.Id,
                CategoryId = category.Id,
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                Price = new Money(request.PriceAmount),
                PriceType = request.PriceType,
                Condition = request.Condition,
                Location = new Location(request.State, request.City.Trim()),
                Attributes = request.Attributes ?? new Dictionary<string, string>(),
                // Probation vendors' listings are pre-moderated (hidden until an admin approves them).
                Status = vendor.TrustTier == TrustTier.Trusted ? ListingStatus.Live : ListingStatus.Unavailable,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await products.AddAsync(product, ct);
            logger.LogInformation("Listing {ProductId} created by vendor {VendorId} (status {Status}).",
                product.Id, vendor.Id, product.Status);

            return ToDetail(product, vendor, category.Name, imageUrls: [], average: 0, reviewCount: 0);
        }

        public async Task<ProductDetail> UpdateAsync(
            Guid userId, Guid productId, UpdateProductRequest request, CancellationToken ct)
        {
            var vendor = await RequireVendor(userId, ct);
            var product = await RequireOwnedProduct(productId, vendor.Id, ct);
            var category = await RequireCategory(request.CategoryId, ct);
            EnsureConditionAllowed(request.Condition, category);

            product.Title = request.Title.Trim();
            product.Description = request.Description.Trim();
            product.Price = new Money(request.PriceAmount);
            product.PriceType = request.PriceType;
            product.Condition = request.Condition;
            product.CategoryId = category.Id;
            product.Location = new Location(request.State, request.City.Trim());
            product.UpdatedAtUtc = DateTimeOffset.UtcNow;

            // Untrusted vendors' edits return to moderation rather than publishing immediately.
            if (vendor.TrustTier != TrustTier.Trusted && product.Status == ListingStatus.Live)
                product.Status = ListingStatus.Unavailable;

            await products.SaveAsync(product, ct);

            var (avg, count) = Rating(product);
            return ToDetail(product, vendor, category.Name, ImageUrls(product), avg, count);
        }

        public async Task ChangeStatusAsync(Guid userId, Guid productId, ChangeStatusRequest request, CancellationToken ct)
        {
            var vendor = await RequireVendor(userId, ct);
            var product = await RequireOwnedProduct(productId, vendor.Id, ct);

            if (request.Status is not (ListingStatus.Live or ListingStatus.Unavailable or ListingStatus.Archived))
                throw new BusinessRuleException("Vendors may only set a listing to Live, Unavailable, or Archived.");

            if (product.Status == ListingStatus.Removed)
                throw new BusinessRuleException("This listing was removed by an administrator and cannot be changed.");

            product.Status = request.Status;
            product.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await products.SaveAsync(product, ct);
        }

        public async Task DeleteAsync(Guid userId, bool isAdmin, Guid productId, CancellationToken ct)
        {
            var product = await products.GetTrackedAsync(productId, ct)
                ?? throw new NotFoundException("Listing not found.");

            if (!isAdmin)
            {
                var vendor = await RequireVendor(userId, ct);
                if (product.VendorProfileId != vendor.Id)
                    throw new ForbiddenException("You can only delete your own listings.");
            }

            product.Status = ListingStatus.Archived;
            product.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await products.SaveAsync(product, ct);
        }

        public async Task<IReadOnlyList<string>> AddImagesAsync(
            Guid userId, Guid productId, IReadOnlyList<ImageUpload> images, CancellationToken ct)
        {
            var vendor = await RequireVendor(userId, ct);
            var product = await RequireOwnedProduct(productId, vendor.Id, ct);

            if (images.Count == 0)
                throw new BusinessRuleException("At least one image is required.");

            var existing = await products.CountImagesAsync(productId, ct);
            if (existing + images.Count > MaxImages)
                throw new BusinessRuleException($"A listing may have at most {MaxImages} images.");

            foreach (var image in images)
            {
                if (image.Length > MaxImageBytes)
                    throw new PayloadTooLargeException("Each image must be 5MB or smaller.");
                // The declared ContentType is client-supplied and spoofable, so it is only a first gate;
                // the actual file bytes must also carry a supported image signature.
                if (!AllowedImageTypes.Contains(image.ContentType)
                    || !await HasSupportedImageSignatureAsync(image, ct))
                    throw new UnsupportedMediaTypeException("Only JPEG, PNG, and WebP images are allowed.");
            }

            var stored = new List<ProductImage>();
            var sortOrder = existing;
            foreach (var image in images)
            {
                var asset = await imageStorage.UploadAsync(image, $"products/{productId}", ct);
                stored.Add(new ProductImage
                {
                    Id = Guid.CreateVersion7(),
                    ProductId = productId,
                    CloudinaryPublicId = asset.PublicId,
                    Url = asset.Url,
                    IsCover = existing == 0 && sortOrder == 0,
                    SortOrder = sortOrder++,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }

            await products.AddImagesAsync(stored, ct);
            return stored.Select(i => i.Url).ToList();
        }

        public async Task<ProductDetail> GetPublicDetailAsync(Guid productId, CancellationToken ct)
        {
            var product = await products.GetDetailAsync(productId, ct);
            if (product is null
                || product.Status != ListingStatus.Live
                || product.Vendor.User.Status != UserStatus.Active)
                throw new NotFoundException("Listing not found.");

            await products.IncrementViewAsync(productId, ct);

            var (avg, count) = Rating(product);
            return ToDetail(product, product.Vendor, product.Category.Name, ImageUrls(product), avg, count);
        }

        // ---- helpers ----

        private async Task<VendorProfile> RequireVendor(Guid userId, CancellationToken ct) =>
            await vendors.GetProfileByUserAsync(userId, ct)
            ?? throw new ForbiddenException("You must be an approved vendor to manage listings.");

        private async Task<Category> RequireCategory(Guid categoryId, CancellationToken ct) =>
            await categories.GetAsync(categoryId, ct)
            ?? throw new BusinessRuleException("The selected category does not exist.");

        private async Task<Product> RequireOwnedProduct(Guid productId, Guid vendorProfileId, CancellationToken ct)
        {
            var product = await products.GetTrackedAsync(productId, ct)
                ?? throw new NotFoundException("Listing not found.");
            if (product.VendorProfileId != vendorProfileId)
                throw new ForbiddenException("You can only manage your own listings.");
            return product;
        }

        private static void EnsureConditionAllowed(ProductCondition condition, Category category)
        {
            var allowed = category.IsConsumable
                ? condition is ProductCondition.New or ProductCondition.Sealed
                : condition is ProductCondition.New or ProductCondition.Used;

            if (!allowed)
                throw new BusinessRuleException(category.IsConsumable
                    ? "Consumable items must be New or Sealed."
                    : "This category supports only New or Used items.");
        }

        /// <summary>
        /// Verifies the leading bytes match a supported image format (JPEG, PNG, or WebP). Defends
        /// against a spoofed <c>Content-Type</c> header smuggling a non-image past the allowlist.
        /// Rewinds the stream afterward so the subsequent upload reads from the start.
        /// </summary>
        private static async Task<bool> HasSupportedImageSignatureAsync(ImageUpload image, CancellationToken ct)
        {
            if (!image.Content.CanSeek)
                return false;

            var header = new byte[12];
            image.Content.Position = 0;
            var read = await image.Content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
            image.Content.Position = 0;
            if (read < header.Length)
                return false;

            // JPEG: FF D8 FF
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return true;
            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
                return true;
            // WebP: "RIFF" <4 bytes> "WEBP"
            if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
                return true;

            return false;
        }

        private static (double Average, int Count) Rating(Product product)
        {
            if (product.Reviews.Count == 0)
                return (0, 0);
            return (Math.Round(product.Reviews.Average(r => r.Rating), 2), product.Reviews.Count);
        }

        private static IReadOnlyList<string> ImageUrls(Product product) =>
            product.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList();

        private static ProductDetail ToDetail(
            Product p, VendorProfile vendor, string categoryName,
            IReadOnlyList<string> imageUrls, double average, int reviewCount) =>
            new(
                p.Id, p.Title, p.Description, p.Price.Amount, p.Price.Currency, p.PriceType, p.Condition,
                categoryName, p.Location.State, p.Location.City, imageUrls,
                vendor.Id, vendor.BusinessName, vendor.VerificationStatus == VerificationStatus.Verified,
                average, reviewCount, p.Status, p.CreatedAtUtc);
    }
}
