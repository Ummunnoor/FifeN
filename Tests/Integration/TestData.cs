using System;
using Domain.Entities.Catalog;
using Domain.Entities.Enums;
using Domain.Entities.Identity;
using Domain.Entities.Interactions;
using Domain.Entities.Vendors;
using Domain.ValueObjects;

namespace FifeN.Tests.Integration
{
    /// <summary>
    /// Builders for a valid User → VendorProfile → Category → Product graph, so integration tests can
    /// stand up realistic listings without repeating the required-field boilerplate. The generated
    /// <c>SearchVector</c> column is left null on purpose — PostgreSQL computes it on insert.
    /// </summary>
    internal static class TestData
    {
        public static User VendorUser(string phone = "+2348020000001") => new()
        {
            Id = Guid.NewGuid(),
            UserName = phone,
            PhoneNumber = phone,
            PhoneNumberConfirmed = true,
            FirstName = "Vendor",
            LastName = "Owner",
            Status = UserStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastActiveAtUtc = DateTimeOffset.UtcNow
        };

        public static User Buyer(string phone = "+2348039999999") => new()
        {
            Id = Guid.NewGuid(),
            UserName = phone,
            PhoneNumber = phone,
            PhoneNumberConfirmed = true,
            FirstName = "Buyer",
            LastName = "Person",
            Status = UserStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastActiveAtUtc = DateTimeOffset.UtcNow
        };

        public static VendorProfile Vendor(
            Guid userId, string businessName = "Test Stores",
            VerificationStatus status = VerificationStatus.Verified) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BusinessName = businessName,
            WhatsAppNumber = "+2348020000001",
            VerificationMethod = VerificationMethod.Nin,
            VerificationStatus = status,
            TrustTier = TrustTier.Trusted,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        public static Category Category(string name, string slug) => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            IsActive = true,
            SortOrder = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        public static Product Product(
            Guid vendorProfileId, Guid categoryId, string title, string description,
            NigerianState state, string city, decimal price = 10000m,
            ListingStatus status = ListingStatus.Live,
            DateTimeOffset? createdAt = null, DateTimeOffset? updatedAt = null) => new()
        {
            Id = Guid.NewGuid(),
            VendorProfileId = vendorProfileId,
            CategoryId = categoryId,
            Title = title,
            Description = description,
            Price = new Money(price),
            PriceType = PriceType.Fixed,
            Condition = ProductCondition.New,
            Location = new Location(state, city),
            Status = status,
            CreatedAtUtc = createdAt ?? DateTimeOffset.UtcNow,
            UpdatedAtUtc = updatedAt ?? DateTimeOffset.UtcNow
        };

        public static Interaction Interaction(
            Guid buyerUserId, Guid vendorProfileId, Guid productId,
            bool isCrossDiscovery, DateTimeOffset? createdAt = null) => new()
        {
            Id = Guid.NewGuid(),
            BuyerUserId = buyerUserId,
            VendorProfileId = vendorProfileId,
            ProductId = productId,
            IsCrossDiscovery = isCrossDiscovery,
            LeadStatus = LeadStatus.New,
            CreatedAtUtc = createdAt ?? DateTimeOffset.UtcNow
        };
    }
}
