using API.RateLimiting;
using Application.Modules.Identity.DTOs;
using Application.Modules.Identity.Services.Interfaces;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers
{
    /// <summary>Phone + OTP authentication endpoints.</summary>
    [ApiController]
    [Route("api/v1/auth")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public class AuthController(
        IAuthenticationService auth, IMfaService mfa, ICurrentUserService currentUser) : ControllerBase
    {
        /// <summary>Sends a one-time passcode to a Nigerian phone number (WhatsApp-first).</summary>
        [HttpPost("otp/request")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestOtp([FromBody] OtpRequestRequest request, CancellationToken ct)
        {
            await auth.RequestOtpAsync(request, ct);
            return Ok();
        }

        /// <summary>Verifies the passcode and returns a session, creating the account on first verify.</summary>
        [HttpPost("otp/verify")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> VerifyOtp([FromBody] OtpVerifyRequest request, CancellationToken ct)
        {
            var response = await auth.VerifyOtpAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            return Ok(response);
        }

        /// <summary>Rotates the refresh token and returns a fresh session.</summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
        {
            var response = await auth.RefreshAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            return Ok(response);
        }

        /// <summary>Revokes the supplied refresh token.</summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken ct)
        {
            await auth.LogoutAsync(request, ct);
            return NoContent();
        }

        /// <summary>Sends an OTP to a proposed recovery (secondary) phone number.</summary>
        [HttpPost("secondary-phone")]
        [Authorize]
        public async Task<IActionResult> AddSecondaryPhone(
            [FromBody] AddSecondaryPhoneRequest request, CancellationToken ct)
        {
            await auth.RequestSecondaryPhoneAsync(currentUser.RequireUserId(), request, ct);
            return Accepted();
        }

        /// <summary>Confirms the OTP and binds the recovery number to the caller's account.</summary>
        [HttpPost("secondary-phone/verify")]
        [Authorize]
        public async Task<IActionResult> VerifySecondaryPhone(
            [FromBody] VerifySecondaryPhoneRequest request, CancellationToken ct)
        {
            await auth.VerifySecondaryPhoneAsync(currentUser.RequireUserId(), request, ct);
            return NoContent();
        }

        /// <summary>Begins TOTP enrollment for an admin; returns the otpauth URI to scan. Authorizes on
        /// the Admin role alone (not the MFA-gated <c>RequireAdmin</c> policy) so MFA can be bootstrapped.</summary>
        [HttpPost("mfa/enroll")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<MfaEnrollResponse>> EnrollMfa(CancellationToken ct) =>
            Ok(await mfa.EnrollAsync(currentUser.RequireUserId(), ct));

        /// <summary>Activates MFA after confirming a code from the authenticator app.</summary>
        [HttpPost("mfa/verify")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> VerifyMfa([FromBody] MfaVerifyRequest request, CancellationToken ct)
        {
            await mfa.VerifyAsync(currentUser.RequireUserId(), request, ct);
            return NoContent();
        }
    }
}
