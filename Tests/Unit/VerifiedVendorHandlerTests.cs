using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Modules.Vendors.Services.Interfaces;
using Domain.Entities.Enums;
using Domain.Entities.Vendors;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Persistence.Authorization;
using Xunit;

namespace FifeN.Tests.Unit
{
    /// <summary>
    /// Unit tests for the DB-backed <see cref="VerifiedVendorHandler"/> that enforces the verified-status
    /// half of the <c>RequireVendor</c> policy (spec §4.1). The vendor repository is mocked so the
    /// handler's claim parsing and verification check are exercised in isolation.
    /// </summary>
    public class VerifiedVendorHandlerTests
    {
        private readonly Mock<IVendorRepository> _vendors = new();
        private readonly VerifiedVendorRequirement _requirement = new();

        private VerifiedVendorHandler CreateSut() => new(_vendors.Object);

        private static AuthorizationHandlerContext ContextFor(
            VerifiedVendorRequirement requirement, params Claim[] claims)
        {
            var identity = new ClaimsIdentity(claims, authenticationType: "test");
            var principal = new ClaimsPrincipal(identity);
            return new AuthorizationHandlerContext(new[] { requirement }, principal, resource: null);
        }

        private static VendorProfile ProfileWith(VerificationStatus status) => new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BusinessName = "Test Vendor",
            WhatsAppNumber = "+2348012345678",
            VerificationStatus = status
        };

        [Fact]
        public async Task HandleRequirementAsync_WhenVendorProfileIsVerified_Succeeds()
        {
            var userId = Guid.NewGuid();
            _vendors.Setup(v => v.GetProfileByUserAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProfileWith(VerificationStatus.Verified));
            var context = ContextFor(_requirement, new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

            await CreateSut().HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        [Theory]
        [InlineData(VerificationStatus.Pending)]
        [InlineData(VerificationStatus.Failed)]
        public async Task HandleRequirementAsync_WhenVendorProfileIsNotVerified_DoesNotSucceed(
            VerificationStatus status)
        {
            var userId = Guid.NewGuid();
            _vendors.Setup(v => v.GetProfileByUserAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProfileWith(status));
            var context = ContextFor(_requirement, new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

            await CreateSut().HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_WhenNoVendorProfileExists_DoesNotSucceed()
        {
            var userId = Guid.NewGuid();
            _vendors.Setup(v => v.GetProfileByUserAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((VendorProfile?)null);
            var context = ContextFor(_requirement, new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

            await CreateSut().HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_WhenNameIdentifierClaimMissing_DoesNotSucceedOrQueryRepository()
        {
            var context = ContextFor(_requirement); // no claims

            await CreateSut().HandleAsync(context);

            Assert.False(context.HasSucceeded);
            _vendors.Verify(
                v => v.GetProfileByUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleRequirementAsync_WhenNameIdentifierClaimIsNotAGuid_DoesNotSucceed()
        {
            var context = ContextFor(_requirement, new Claim(ClaimTypes.NameIdentifier, "not-a-guid"));

            await CreateSut().HandleAsync(context);

            Assert.False(context.HasSucceeded);
            _vendors.Verify(
                v => v.GetProfileByUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
