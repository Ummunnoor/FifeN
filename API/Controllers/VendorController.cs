using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Modules.Vendors.DTOs;
using Application.Modules.Vendors.Services.Interfaces;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>Vendor onboarding, self-service profile, and public shop header.</summary>
    [ApiController]
    [Route("api/v1")]
    public class VendorController(IVendorService vendors, ICurrentUserService currentUser) : ControllerBase
    {
        /// <summary>Submits a vendor onboarding application (runs KYC).</summary>
        [HttpPost("vendor/requests")]
        [Authorize]
        public async Task<ActionResult<VendorRequestResponse>> SubmitRequest(
            [FromBody] CreateVendorRequestRequest request, CancellationToken ct)
        {
            var response = await vendors.SubmitRequestAsync(currentUser.RequireUserId(), request, ct);
            return CreatedAtAction(nameof(GetMyRequest), new { }, response);
        }

        /// <summary>Returns the caller's latest vendor application.</summary>
        [HttpGet("vendor/requests/me")]
        [Authorize]
        public async Task<ActionResult<VendorRequestResponse>> GetMyRequest(CancellationToken ct) =>
            Ok(await vendors.GetMyLatestRequestAsync(currentUser.RequireUserId(), ct));

        /// <summary>Updates the caller's vendor profile (WhatsApp number). Role-only per access
        /// matrix §4.2: a vendor may correct their profile even if verification was later revoked;
        /// the verified gate (<c>RequireVendor</c>) applies to listings and leads, not this.</summary>
        [HttpPatch("vendor/profile")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> UpdateProfile(
            [FromBody] UpdateVendorProfileRequest request, CancellationToken ct)
        {
            await vendors.UpdateProfileAsync(currentUser.RequireUserId(), request, ct);
            return Ok();
        }

        /// <summary>Public shop header for a vendor.</summary>
        [HttpGet("vendors/{id:guid}")]
        [AllowAnonymous]
        public async Task<ActionResult<VendorPublicResponse>> GetPublic(Guid id, CancellationToken ct) =>
            Ok(await vendors.GetPublicAsync(id, ct));
    }
}
