using System;
using System.Collections.Generic;
using Domain.Entities.Enums;

namespace Application.Modules.Catalog.DTOs
{
    /// <summary>Create-a-listing payload. Price is NGN; location is State + free-text City.</summary>
    public record CreateProductRequest(
        string Title,
        string Description,
        decimal PriceAmount,
        PriceType PriceType,
        ProductCondition Condition,
        Guid CategoryId,
        NigerianState State,
        string City,
        Dictionary<string, string>? Attributes);

    /// <summary>Update-a-listing payload (attributes are managed separately).</summary>
    public record UpdateProductRequest(
        string Title,
        string Description,
        decimal PriceAmount,
        PriceType PriceType,
        ProductCondition Condition,
        Guid CategoryId,
        NigerianState State,
        string City);

    /// <summary>Vendor status transition for a listing (Unavailable / Live / Archived).</summary>
    public record ChangeStatusRequest(ListingStatus Status);

    /// <summary>Admin moderation decision for a listing.</summary>
    public record ModerateProductRequest(ListingStatus Status, string Reason);

    /// <summary>Compact listing projection for grids and search results.</summary>
    public record ProductSummary(
        Guid Id,
        string Title,
        decimal PriceAmount,
        string Currency,
        PriceType PriceType,
        string? CoverUrl,
        string City,
        NigerianState State,
        Guid VendorId,
        string VendorName,
        bool VendorVerified,
        double AverageRating,
        ListingStatus Status);

    /// <summary>Full listing projection for the detail page.</summary>
    public record ProductDetail(
        Guid Id,
        string Title,
        string Description,
        decimal PriceAmount,
        string Currency,
        PriceType PriceType,
        ProductCondition Condition,
        string CategoryName,
        NigerianState State,
        string City,
        IReadOnlyList<string> ImageUrls,
        Guid VendorId,
        string VendorName,
        bool VendorVerified,
        double AverageRating,
        int ReviewCount,
        ListingStatus Status,
        DateTimeOffset CreatedAtUtc,
        /// <summary>True when the authenticated caller owns this listing — UI hides the buyer CTAs.</summary>
        bool IsOwnListing);

    /// <summary>Active category for selection and discovery.</summary>
    public record CategoryResponse(Guid Id, string Name, string Slug, bool IsConsumable);
}
