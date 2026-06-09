using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Application.Modules.Vendors.Services.Interfaces;
using Domain.Entities.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Persistence.Authorization
{
    /// <summary>
    /// Requirement backing the <c>RequireVendor</c> policy's verified-status half (spec §4.1):
    /// the caller must own a <see cref="Domain.Entities.Vendors.VendorProfile"/> whose
    /// <see cref="VerificationStatus"/> is <see cref="VerificationStatus.Verified"/>. The Vendor
    /// role itself is asserted separately via <c>RequireRole</c> on the same policy.
    /// </summary>
    public sealed class VerifiedVendorRequirement : IAuthorizationRequirement
    {
    }

    /// <summary>
    /// Resolves the caller's vendor profile from the database on each authorization, so a vendor
    /// who is suspended or whose verification is revoked is blocked immediately — not only after
    /// their short-lived access token expires. Mirrors the resource-handler pattern used elsewhere.
    /// </summary>
    public sealed class VerifiedVendorHandler(IVendorRepository vendors)
        : AuthorizationHandler<VerifiedVendorRequirement>
    {
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, VerifiedVendorRequirement requirement)
        {
            var idClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(idClaim, out var userId))
                return; // unauthenticated / malformed token — leave the requirement unmet.

            var profile = await vendors.GetProfileByUserAsync(userId, default);
            if (profile is { VerificationStatus: VerificationStatus.Verified })
                context.Succeed(requirement);
        }
    }
}
